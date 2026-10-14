using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RagApp.Domain;

namespace RagApp.Application
{
    public class RagService : IRagService
    {
        private readonly IVectorStore _vectorStore;
        private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator;
        private readonly IChatClient _chatClient;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly RagOptions _options;
        private readonly ILogger<RagService> _logger;
        private readonly System.Threading.SemaphoreSlim _statusGate = new(1, 1);

        // Shared across request scopes: tracks in-flight ingestion jobs so they can be
        // cancelled from any scope (e.g. when a document is deleted) and so background
        // work is not bound to a request's request-scoped CancellationToken.
        private static readonly ConcurrentDictionary<Guid, JobState> _jobs = new();

        private sealed record JobState(CancellationTokenSource Cts, string Content);

        public RagService(
            IVectorStore vectorStore,
            IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
            IChatClient chatClient,
            IServiceScopeFactory scopeFactory,
            IOptions<RagOptions> options,
            ILogger<RagService> logger)
        {
            _vectorStore = vectorStore;
            _embeddingGenerator = embeddingGenerator;
            _chatClient = chatClient;
            _scopeFactory = scopeFactory;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<Guid> StartIngestionAsync(string title, string content, CancellationToken cancellationToken = default)
        {
            ValidateForIngestion(title, content, out var cleanTitle, out var cleanContent);

            var document = new Document
            {
                Title = cleanTitle,
                Content = cleanContent,
                CreatedAt = DateTime.UtcNow,
                Status = "queued"
            };

            var documentId = await _vectorStore.CreateDocumentAsync(document, cancellationToken);

            var cts = new CancellationTokenSource();
            _jobs[documentId] = new JobState(cts, cleanContent);

            // Fire-and-forget background ingestion with its own DI scope so the request
            // can return immediately (async upload) and failures are captured on the row.
            _ = Task.Run(async () =>
            {
                using var scope = _scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<IRagService>();
                try
                {
                    await service.ProcessIngestionAsync(documentId, cts.Token);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Background ingestion failed unexpectedly for document {DocumentId}.", documentId);
                }
            });

            return documentId;
        }

        public async Task ProcessIngestionAsync(Guid documentId, CancellationToken cancellationToken = default)
        {
            if (!_jobs.TryGetValue(documentId, out var state))
            {
                return;
            }

            // Merge with the provided token so callers can also cancel.
            var token = cancellationToken;
            if (token == CancellationToken.None)
            {
                token = state.Cts.Token;
            }

            int totalChunks = 0;
            try
            {
                var textChunks = ChunkText(state.Content, _options.ChunkSize, _options.ChunkOverlap);
                if (textChunks.Count == 0)
                {
                    throw new ArgumentException("Document content is empty after chunking.");
                }
                totalChunks = textChunks.Count;

                await SetStatusAsync(documentId, "processing", error: null, totalChunks, 0, token);

                var embeddings = await EmbedAllAsync(documentId, textChunks, token);

                var chunkEntities = new List<DocumentChunk>(textChunks.Count);
                for (int i = 0; i < textChunks.Count; i++)
                {
                    chunkEntities.Add(new DocumentChunk
                    {
                        DocumentId = documentId,
                        Content = textChunks[i],
                        ChunkIndex = i,
                        Embedding = embeddings[i].Vector.ToArray()
                    });
                }

                await _vectorStore.AddChunksAsync(chunkEntities, token);
                await SetStatusAsync(documentId, "ready", error: null, totalChunks, totalChunks, token);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested || state.Cts.IsCancellationRequested)
            {
                await TrySetFailedAsync(documentId, totalChunks, "Ingestion was cancelled (the document may have been deleted).");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ingestion failed for document {DocumentId}.", documentId);
                await TrySetFailedAsync(documentId, totalChunks, ex.Message);
            }
            finally
            {
                _jobs.TryRemove(documentId, out _);
                state.Cts.Dispose();
            }
        }

        private async Task<Embedding<float>[]> EmbedAllAsync(Guid documentId, IReadOnlyList<string> texts, CancellationToken cancellationToken)
        {
            int batchSize = Math.Max(1, _options.EmbeddingBatchSize);
            int batchCount = (texts.Count + batchSize - 1) / batchSize;
            int progressEvery = Math.Max(1, _options.ProgressUpdateEvery);

            var embeddings = new Embedding<float>[texts.Count];
            var completed = 0;

            await Parallel.ForEachAsync(
                Enumerable.Range(0, batchCount),
                new ParallelOptions
                {
                    MaxDegreeOfParallelism = Math.Max(1, _options.EmbeddingConcurrency),
                    CancellationToken = cancellationToken
                },
                async (batch, token) =>
                {
                    int start = batch * batchSize;
                    int length = Math.Min(batchSize, texts.Count - start);

                    var batchTexts = new string[length];
                    for (int i = 0; i < length; i++)
                    {
                        batchTexts[i] = texts[start + i];
                    }

                    var result = await AiGuardAsync(
                        () => _embeddingGenerator.GenerateAsync(batchTexts, cancellationToken: token),
                        "generating embeddings",
                        token);

                    int index = 0;
                    foreach (var embedding in result)
                    {
                        embeddings[start + index++] = embedding;
                    }

                    var done = Interlocked.Add(ref completed, length);
                    if (done == texts.Count || done % progressEvery < length)
                    {
                        await SetStatusAsync(documentId, "processing", error: null, texts.Count, done, token);
                    }
                });

            return embeddings;
        }

        private static void ValidateForIngestion(string title, string content, out string cleanTitle, out string cleanContent)
        {
            if (string.IsNullOrWhiteSpace(title))
                throw new ArgumentException("Title is required.", nameof(title));
            if (string.IsNullOrWhiteSpace(content))
                throw new ArgumentException("Content is required.", nameof(content));

            // PostgreSQL text columns reject NUL bytes (22021); strip them before
            // chunking/embedding so binary or Office-sourced text can't fail the insert.
            cleanTitle = title.Trim().Replace("\0", string.Empty);
            cleanContent = content.Trim().Replace("\0", string.Empty);

            // Raw PDF bytes should never reach the store: the client is expected to
            // extract text (pdf.js) before uploading. Reject so we don't embed garbage.
            if (cleanContent.StartsWith("%PDF-", StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "The uploaded file appears to be a raw PDF. PDF text could not be extracted; " +
                    "please upload a .pdf file from the frontend or convert it to text first.",
                    nameof(content));
            }

            // .xlsx/.docx are zip archives (PK\x03\x04). The client must extract cell text
            // before uploading; reject raw archives so we don't embed binary garbage.
            if (cleanContent.Length >= 4
                && cleanContent[0] == 'P' && cleanContent[1] == 'K'
                && cleanContent[2] == '\x03' && cleanContent[3] == '\x04')
            {
                throw new ArgumentException(
                    "The uploaded file appears to be a raw binary archive (e.g. .xlsx/.docx). " +
                    "Please upload the file from the frontend so its text can be extracted.",
                    nameof(content));
            }

            if (string.IsNullOrWhiteSpace(cleanTitle))
                throw new ArgumentException("Title is required.", nameof(title));
            if (string.IsNullOrWhiteSpace(cleanContent))
                throw new ArgumentException("Document content is empty.", nameof(content));
        }

        public async Task<DocumentInfo?> GetDocumentAsync(Guid documentId, CancellationToken cancellationToken = default)
        {
            return await _vectorStore.GetDocumentAsync(documentId, cancellationToken);
        }

        public async Task<IReadOnlyList<DocumentInfo>> GetDocumentsAsync(CancellationToken cancellationToken = default)
        {
            return await _vectorStore.GetDocumentsAsync(cancellationToken);
        }

        public async Task DeleteDocumentAsync(Guid documentId, CancellationToken cancellationToken = default)
        {
            // Cancel any in-flight ingestion before removing the row so the background
            // job doesn't keep embedding/inserting chunks for a deleted document.
            if (_jobs.TryGetValue(documentId, out var state))
            {
                state.Cts.Cancel();
            }

            var deleted = await _vectorStore.DeleteDocumentAsync(documentId, cancellationToken);
            if (!deleted)
            {
                throw new KeyNotFoundException($"Document '{documentId}' was not found.");
            }

            _jobs.TryRemove(documentId, out _);
        }

        public async Task<string> AskAsync(string question, IReadOnlyCollection<Guid>? documentIds = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(question))
                throw new ArgumentException("Question is required.", nameof(question));

            var questionEmbeddingResult = await AiGuardAsync(
                () => _embeddingGenerator.GenerateAsync([question], cancellationToken: cancellationToken),
                "generating the question embedding",
                cancellationToken);
            var queryVector = questionEmbeddingResult[0].Vector.ToArray();

            // 8 because chunks are now whole rows (~515 chars each): more complete
            // records in context means better anchoring instead of noisy fragments.
            var relevantChunks = await _vectorStore.SearchAsync(queryVector, matchCount: 8, documentIds, cancellationToken);

            var contextBuilder = new StringBuilder();
            foreach (var chunk in relevantChunks)
            {
                contextBuilder.AppendLine(chunk.Content);
                contextBuilder.AppendLine("---");
            }

            var prompt = $"""
                You are a helpful AI assistant. Answer the user's question using ONLY the provided context snippets. If you don't know the answer, say "I don't know based on the provided documents."

                Context:
                {contextBuilder}

                Question: {question}
                """;

            var response = await AiGuardAsync(
                () => _chatClient.GetResponseAsync(prompt, cancellationToken: cancellationToken),
                "generating the answer",
                cancellationToken);
            return response.Text ?? "No response generated.";
        }

        private async Task SetStatusAsync(Guid documentId, string status, string? error, int totalChunks, int chunksCompleted, CancellationToken cancellationToken)
        {
            await _statusGate.WaitAsync(cancellationToken);
            try
            {
                await _vectorStore.UpdateDocumentAsync(new Document
                {
                    Id = documentId,
                    Status = status,
                    Error = error,
                    TotalChunks = totalChunks,
                    ChunksCompleted = chunksCompleted
                }, cancellationToken);
            }
            finally
            {
                _statusGate.Release();
            }
        }

        private async Task TrySetFailedAsync(Guid documentId, int totalChunks, string message)
        {
            try
            {
                await SetStatusAsync(documentId, "failed", message, totalChunks, 0, CancellationToken.None);
            }
            catch (Exception ex)
            {
                // Row may already be gone (e.g. cancelled + deleted mid-ingestion); make it observable.
                _logger.LogWarning(ex, "Could not mark document {DocumentId} as failed.", documentId);
            }
        }

        private static async Task<T> AiGuardAsync<T>(Func<Task<T>> action, string operation, CancellationToken cancellationToken)
        {
            try
            {
                return await action();
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new AIGatewayException(
                    $"The AI gateway ({ex.Message}) is unreachable or returned an error while {operation}. " +
                    "Verify Ollama is running and that AI_ROUTER_URL / AI_API_KEY are configured correctly.",
                    ex);
            }
        }

        private static List<string> ChunkText(string text, int maxCharacters, int overlap)
        {
            var chunks = new List<string>();
            if (string.IsNullOrWhiteSpace(text)) return chunks;

            // Line-aware chunking: never split a logical record. Spreadsheets emit one
            // row per line ("Label: value, ..."), so a violated record must stay whole
            // for retrieval to return complete rows. Overlap is deliberately ignored
            // here because re-sending a row's tail would duplicate whole records.
            _ = overlap;

            var lines = text.Split('\n');
            var current = new StringBuilder();

            // Hard safety cap (so a pathological single line can still be embedded);
            // wide-but-normal rows (a few hundred chars) are kept intact.
            int maxLine = Math.Max(maxCharacters * 4, 2000);

            foreach (var rawLine in lines)
            {
                var lineText = rawLine.TrimEnd('\r');
                if (lineText.Length == 0 && current.Length == 0)
                {
                    continue;
                }

                if (lineText.Length > maxLine)
                {
                    if (current.Length > 0)
                    {
                        chunks.Add(current.ToString());
                        current.Clear();
                    }
                    // Fall back to the old fixed-window splitting for a single oversized
                    // line so embedding never receives an unbounded block.
                    int start = 0;
                    while (start < lineText.Length)
                    {
                        int length = Math.Min(maxCharacters, lineText.Length - start);
                        chunks.Add(lineText.Substring(start, length));
                        start += maxCharacters;
                    }
                    continue;
                }

                if (current.Length == 0)
                {
                    current.Append(lineText);
                    continue;
                }

                // +1 for the newline re-inserted between lines.
                if (current.Length + 1 + lineText.Length <= maxCharacters)
                {
                    current.Append('\n').Append(lineText);
                    continue;
                }

                chunks.Add(current.ToString());
                current.Clear();
                current.Append(lineText);
            }

            if (current.Length > 0)
            {
                chunks.Add(current.ToString());
            }

            return chunks;
        }
    }
}