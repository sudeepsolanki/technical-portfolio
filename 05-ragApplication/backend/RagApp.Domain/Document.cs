using System;
using System.Collections.Generic;

namespace RagApp.Domain
{
    public class Document
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string Status { get; set; } = "queued"; // queued | processing | ready | failed
        public string? Error { get; set; }
        public int TotalChunks { get; set; }
        public int ChunksCompleted { get; set; }
        public ICollection<DocumentChunk> Chunks { get; set; } = new List<DocumentChunk>();
    }
}
