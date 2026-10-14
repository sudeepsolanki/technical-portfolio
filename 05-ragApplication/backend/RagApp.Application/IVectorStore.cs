using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RagApp.Domain;

namespace RagApp.Application
{
    public interface IVectorStore
    {
        Task AddChunksAsync(IEnumerable<DocumentChunk> chunks, CancellationToken cancellationToken = default);
        Task<Guid> CreateDocumentAsync(Document document, CancellationToken cancellationToken = default);
        Task UpdateDocumentAsync(Document document, CancellationToken cancellationToken = default);
        Task<DocumentInfo?> GetDocumentAsync(Guid documentId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<DocumentInfo>> GetDocumentsAsync(CancellationToken cancellationToken = default);
        Task<bool> DeleteDocumentAsync(Guid documentId, CancellationToken cancellationToken = default);
        Task<IEnumerable<DocumentChunk>> SearchAsync(
            float[] queryEmbedding,
            int matchCount = 5,
            IReadOnlyCollection<Guid>? documentIds = null,
            CancellationToken cancellationToken = default);
    }
}