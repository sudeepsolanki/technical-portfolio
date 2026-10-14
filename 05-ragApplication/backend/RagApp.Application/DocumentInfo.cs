using System;

namespace RagApp.Application
{
    public record DocumentInfo(
        Guid Id,
        string Title,
        DateTime CreatedAt,
        int ChunkCount,
        string Status,
        string? Error,
        int TotalChunks,
        int ChunksCompleted)
    {
        public bool IsReady => Status == "ready";
    }
}