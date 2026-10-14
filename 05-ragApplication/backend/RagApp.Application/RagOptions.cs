namespace RagApp.Application
{
    public class RagOptions
    {
        public const string SectionName = "Rag";

        /// <summary>Number of chunk texts sent to the embedding API per request (fewer round trips).</summary>
        public int EmbeddingBatchSize { get; set; } = 16;

        /// <summary>Batches of embeddings run in parallel (each batch is one embedding HTTP call).</summary>
        public int EmbeddingConcurrency { get; set; } = 2;

        /// <summary>Chunk size for text chunking.</summary>
        public int ChunkSize { get; set; } = 500;

        /// <summary>Overlap between consecutive chunks.</summary>
        public int ChunkOverlap { get; set; } = 50;

        /// <summary>Persist progress at most once every N embedded chunks.</summary>
        public int ProgressUpdateEvery { get; set; } = 64;
    }
}