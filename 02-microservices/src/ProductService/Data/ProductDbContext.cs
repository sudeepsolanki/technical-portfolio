using Microsoft.EntityFrameworkCore;
using Shared.Contracts.Services;

namespace ProductService.Data;

public class ProductDbContext : DbContext
{
    public ProductDbContext(DbContextOptions<ProductDbContext> options) : base(options) { }

    public DbSet<Product> Products { get; set; } = null!;
    public DbSet<StockReservation> StockReservations { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Product>().HasKey(p => p.Id);
        modelBuilder.Entity<Product>().Property(p => p.Id).ValueGeneratedNever();
        modelBuilder.Entity<Product>().Property(p => p.Name).HasMaxLength(200);
        modelBuilder.Entity<Product>().Property(p => p.Description).HasMaxLength(1000);
        modelBuilder.Entity<Product>().Property(p => p.Category).HasMaxLength(100);

        modelBuilder.Entity<StockReservation>().HasKey(r => r.Id);
        modelBuilder.Entity<StockReservation>().Property(r => r.Id).ValueGeneratedNever();
        modelBuilder.Entity<StockReservation>().HasIndex(r => new { r.ProductId, r.OrderId }).IsUnique();
    }

    public static void SeedData(ProductDbContext context)
    {
        if (context.Products.Any()) return;

        context.Products.AddRange(
            new Product { Id = "prod-001", Name = "Laptop Pro", Description = "High-performance laptop", Price = 1299.99m, StockQuantity = 50, Category = "Electronics" },
            new Product { Id = "prod-002", Name = "Wireless Mouse", Description = "Ergonomic wireless mouse", Price = 49.99m, StockQuantity = 200, Category = "Electronics" },
            new Product { Id = "prod-003", Name = "Mechanical Keyboard", Description = "RGB mechanical keyboard", Price = 129.99m, StockQuantity = 100, Category = "Electronics" },
            new Product { Id = "prod-004", Name = "4K Monitor", Description = "27-inch 4K display", Price = 599.99m, StockQuantity = 75, Category = "Electronics" },
            new Product { Id = "prod-005", Name = "USB-C Hub", Description = "7-in-1 USB-C hub", Price = 79.99m, StockQuantity = 150, Category = "Accessories" },
            new Product { Id = "prod-006", Name = "Webcam HD", Description = "1080p webcam", Price = 89.99m, StockQuantity = 120, Category = "Accessories" },
            new Product { Id = "prod-007", Name = "Headset Pro", Description = "Noise-cancelling headset", Price = 199.99m, StockQuantity = 80, Category = "Audio" },
            new Product { Id = "prod-008", Name = "Desk Lamp", Description = "LED desk lamp", Price = 39.99m, StockQuantity = 250, Category = "Office" },
            new Product { Id = "prod-009", Name = "Ergonomic Chair", Description = "Adjustable office chair", Price = 449.99m, StockQuantity = 30, Category = "Furniture" },
            new Product { Id = "prod-010", Name = "Standing Desk", Description = "Electric standing desk", Price = 699.99m, StockQuantity = 40, Category = "Furniture" }
        );

        context.SaveChanges();
    }
}

public class Product
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public string Category { get; set; } = string.Empty;
}

public class StockReservation
{
    public Guid Id { get; set; }
    public string ProductId { get; set; } = string.Empty;
    public string OrderId { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public DateTime ReservedAt { get; set; }
    public bool IsActive { get; set; } = true;
}
