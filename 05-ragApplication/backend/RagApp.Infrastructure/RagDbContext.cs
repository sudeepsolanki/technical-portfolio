using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Configuration;
using Pgvector;
using RagApp.Domain;

namespace RagApp.Infrastructure
{
    public class RagDbContext : DbContext
    {
        private readonly int _embeddingDimension;

        public DbSet<Document> Documents => Set<Document>();
        public DbSet<DocumentChunk> DocumentChunks => Set<DocumentChunk>();

        public RagDbContext(DbContextOptions<RagDbContext> options, IConfiguration configuration) : base(options)
        {
            var dimension = configuration["Rag:EmbeddingDimension"];
            _embeddingDimension = int.TryParse(dimension, out var parsed) ? parsed : 1536;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.HasPostgresExtension("vector");

            modelBuilder.Entity<Document>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Content).IsRequired();
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.Status).IsRequired().HasMaxLength(20);
                entity.Property(e => e.Error).HasMaxLength(2000);

                entity.HasMany(e => e.Chunks)
                      .WithOne(e => e.Document)
                      .HasForeignKey(e => e.DocumentId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<DocumentChunk>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Content).IsRequired();
                entity.Property(e => e.ChunkIndex).IsRequired();

                // Keep the domain layer free of infra types: float[] <-> pgvector.Vector
                entity.Property(e => e.Embedding)
                      .HasColumnType($"vector({_embeddingDimension})")
                      .HasConversion(
                          v => new Vector(v),
                          v => v.ToArray());

                entity.Property(e => e.Embedding).Metadata.SetValueComparer(new ValueComparer<float[]>(
                    (a, b) => a.SequenceEqual(b),
                    v => v.Aggregate(0, (hash, f) => HashCode.Combine(hash, f)),
                    v => (float[])v.Clone()));
            });
        }
    }
}