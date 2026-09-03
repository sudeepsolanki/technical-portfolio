using Microsoft.EntityFrameworkCore;
using PaymentService.Data;

namespace PaymentService.Data;

public class PaymentDbContext : DbContext
{
    public PaymentDbContext(DbContextOptions<PaymentDbContext> options) : base(options) { }

    public DbSet<PaymentRecord> Payments { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<PaymentRecord>().HasKey(p => p.Id);
        modelBuilder.Entity<PaymentRecord>().Property(p => p.Id).ValueGeneratedNever();
        modelBuilder.Entity<PaymentRecord>().Property(p => p.Amount).HasPrecision(18, 2);
        modelBuilder.Entity<PaymentRecord>().HasIndex(p => p.OrderId);
    }
}

public class PaymentRecord
{
    public Guid Id { get; set; }
    public string OrderId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public string PaymentMethod { get; set; } = "credit_card";
    public string Status { get; set; } = "Pending";
    public DateTime ProcessedAt { get; set; }
    public string? FailureReason { get; set; }
}
