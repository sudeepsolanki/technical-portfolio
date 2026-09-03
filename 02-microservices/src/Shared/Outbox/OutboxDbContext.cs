using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Shared.Messaging;

namespace Shared.Outbox;

public abstract class OutboxDbContext : DbContext
{
    protected OutboxDbContext(DbContextOptions options) : base(options) { }

    public DbSet<OutboxMessage> OutboxMessages { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<OutboxMessage>().HasKey(x => x.Id);
        modelBuilder.Entity<OutboxMessage>().Property(x => x.Id).ValueGeneratedNever();
    }

    public async Task AddOutboxMessageAsync(object message, string correlationId, CancellationToken cancellationToken = default)
    {
        var outboxMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            MessageType = message.GetType().AssemblyQualifiedName!,
            Payload = System.Text.Json.JsonSerializer.Serialize(message),
            CorrelationId = correlationId,
            CreatedAt = DateTime.UtcNow
        };

        OutboxMessages.Add(outboxMessage);
        await SaveChangesAsync(cancellationToken);
    }

    public async Task<List<OutboxMessage>> GetPendingMessagesAsync(CancellationToken cancellationToken = default)
    {
        return await OutboxMessages
            .Where(m => !m.Processed)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task MarkAsProcessedAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        var message = await OutboxMessages.FindAsync(new object[] { messageId }, cancellationToken);
        if (message != null)
        {
            message.Processed = true;
            message.ProcessedAt = DateTime.UtcNow;
            await SaveChangesAsync(cancellationToken);
        }
    }
}

public class OutboxMessage
{
    public Guid Id { get; set; }
    public string MessageType { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    public bool Processed { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
}
