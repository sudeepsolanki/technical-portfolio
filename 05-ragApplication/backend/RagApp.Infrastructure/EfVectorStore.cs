using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;
using RagApp.Application;
using RagApp.Domain;

namespace RagApp.Infrastructure
{
    public class EfVectorStore : IVectorStore
    {
        private readonly RagDbContext _context;

        public EfVectorStore(RagDbContext context)
        {
            _context = context;
        }

        public async Task AddChunksAsync(IEnumerable<DocumentChunk> chunks, CancellationToken cancellationToken = default)
        {
            _context.DocumentChunks.AddRange(chunks);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<Guid> CreateDocumentAsync(Document document, CancellationToken cancellationToken = default)
        {
            _context.Documents.Add(document);
            await _context.SaveChangesAsync(cancellationToken);
            return document.Id;
        }

        // Updates only the progress/status columns of a document; Content/Title are untouched.
        // Reuses the already-tracked entity (if any) instead of blind Attach, which would throw
        // "already being tracked" on the second consecutive update of the same row.
        public async Task UpdateDocumentAsync(Document document, CancellationToken cancellationToken = default)
        {
            var target = _context.Documents.Local.FirstOrDefault(d => d.Id == document.Id);
            if (target is null)
            {
                _context.Documents.Attach(document);
                target = document;
            }

            target.Status = document.Status;
            target.Error = document.Error;
            target.TotalChunks = document.TotalChunks;
            target.ChunksCompleted = document.ChunksCompleted;

            // Mark ONLY the progress/status columns modified. Attach() marks an entity
            // Unchanged, so values set on the DTO are otherwise dropped from the UPDATE.
            // We must not mark the whole entity Modified: the caller passes a minimal DTO
            // whose Title/Content are empty, and doing so would overwrite the stored text
            // with empty strings (documents lost their Title and Content on every ingest).
            var entry = _context.Entry(target);
            entry.Property(e => e.Status).IsModified = true;
            entry.Property(e => e.Error).IsModified = true;
            entry.Property(e => e.TotalChunks).IsModified = true;
            entry.Property(e => e.ChunksCompleted).IsModified = true;

            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<DocumentInfo?> GetDocumentAsync(Guid documentId, CancellationToken cancellationToken = default)
        {
            return await _context.Documents
                .Where(d => d.Id == documentId)
                .Select(d => new DocumentInfo(d.Id, d.Title, d.CreatedAt, d.Chunks.Count, d.Status, d.Error, d.TotalChunks, d.ChunksCompleted))
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<DocumentInfo>> GetDocumentsAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Documents
                .OrderByDescending(d => d.CreatedAt)
                .Select(d => new DocumentInfo(d.Id, d.Title, d.CreatedAt, d.Chunks.Count, d.Status, d.Error, d.TotalChunks, d.ChunksCompleted))
                .ToListAsync(cancellationToken);
        }

        public async Task<bool> DeleteDocumentAsync(Guid documentId, CancellationToken cancellationToken = default)
        {
            var document = await _context.Documents.FirstOrDefaultAsync(d => d.Id == documentId, cancellationToken);
            if (document is null)
            {
                return false;
            }

            _context.Documents.Remove(document);
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<IEnumerable<DocumentChunk>> SearchAsync(
            float[] queryEmbedding,
            int matchCount = 5,
            IReadOnlyCollection<Guid>? documentIds = null,
            CancellationToken cancellationToken = default)
        {
            var vector = new Vector(queryEmbedding);

            var query = _context.DocumentChunks.AsQueryable();

            if (documentIds is { Count: > 0 })
            {
                query = query.Where(c => documentIds.Contains(c.DocumentId));
            }

            return await query
                .OrderBy(c => c.Embedding.CosineDistance(vector))
                .Take(matchCount)
                .Include(c => c.Document)
                .ToListAsync(cancellationToken);
        }
    }
}