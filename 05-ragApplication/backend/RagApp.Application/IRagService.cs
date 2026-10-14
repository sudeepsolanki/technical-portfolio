using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RagApp.Application
{
    public interface IRagService
    {
        /// <summary>Validates input, persists the document (status "queued") and starts background ingestion.</summary>
        Task<Guid> StartIngestionAsync(string title, string content, CancellationToken cancellationToken = default);

        /// <summary>Runs the chunk → embed → store job for a document (executed in a background scope).</summary>
        Task ProcessIngestionAsync(Guid documentId, CancellationToken cancellationToken = default);

        Task<DocumentInfo?> GetDocumentAsync(Guid documentId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<DocumentInfo>> GetDocumentsAsync(CancellationToken cancellationToken = default);
        Task DeleteDocumentAsync(Guid documentId, CancellationToken cancellationToken = default);
        Task<string> AskAsync(string question, IReadOnlyCollection<Guid>? documentIds = null, CancellationToken cancellationToken = default);
    }
}