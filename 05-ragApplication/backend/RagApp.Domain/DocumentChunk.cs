using System;

namespace RagApp.Domain
{
    public class DocumentChunk
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid DocumentId { get; set; }
        public Document Document { get; set; } = null!;
        public string Content { get; set; } = string.Empty;
        public int ChunkIndex { get; set; }
        public float[] Embedding { get; set; } = Array.Empty<float>();
    }
}