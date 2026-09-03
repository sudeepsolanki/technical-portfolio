using Microsoft.EntityFrameworkCore;
using ProductService.Data;
using Shared.Contracts.Services;

namespace ProductService.Services;

public class ProductServiceClient : IProductService
{
    private readonly ProductDbContext _context;

    public ProductServiceClient(ProductDbContext context)
    {
        _context = context;
    }

    public async Task<ProductResponse?> GetProductAsync(string productId)
    {
        var product = await _context.Products.FindAsync(productId);
        if (product == null) return null;
        return new ProductResponse(product.Id, product.Name, product.Description, product.Price, product.StockQuantity, product.Category);
    }

    public async Task<List<ProductResponse>> GetProductsAsync(int page = 0, int pageSize = 20)
    {
        var products = await _context.Products
            .Skip(page * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return products.Select(p => new ProductResponse(p.Id, p.Name, p.Description, p.Price, p.StockQuantity, p.Category)).ToList();
    }

    public async Task<StockResponse> ReserveStockAsync(string productId, int quantity, string orderId)
    {
        var product = await _context.Products.FindAsync(productId);
        if (product == null)
            return new StockResponse(false, $"Product {productId} not found");

        var existingReservation = await _context.StockReservations
            .FirstOrDefaultAsync(r => r.ProductId == productId && r.OrderId == orderId && r.IsActive);
        if (existingReservation != null)
            return new StockResponse(false, "Stock already reserved for this order");

        if (product.StockQuantity < quantity)
            return new StockResponse(false, $"Insufficient stock. Available: {product.StockQuantity}");

        product.StockQuantity -= quantity;
        _context.StockReservations.Add(new StockReservation
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            OrderId = orderId,
            Quantity = quantity,
            ReservedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
        return new StockResponse(true, "Stock reserved successfully");
    }

    public async Task<StockResponse> ReleaseStockAsync(string productId, int quantity, string orderId)
    {
        var reservation = await _context.StockReservations
            .FirstOrDefaultAsync(r => r.ProductId == productId && r.OrderId == orderId && r.IsActive);
        if (reservation == null)
            return new StockResponse(false, "No active reservation found");

        var product = await _context.Products.FindAsync(productId);
        if (product != null)
        {
            product.StockQuantity += reservation.Quantity;
        }

        reservation.IsActive = false;
        await _context.SaveChangesAsync();
        return new StockResponse(true, "Stock released successfully");
    }
}
