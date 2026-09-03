namespace Shared.Contracts.Services;

public interface IProductService
{
    Task<ProductResponse?> GetProductAsync(string productId);
    Task<List<ProductResponse>> GetProductsAsync(int page = 0, int pageSize = 20);
    Task<StockResponse> ReserveStockAsync(string productId, int quantity, string orderId);
    Task<StockResponse> ReleaseStockAsync(string productId, int quantity, string orderId);
}

public interface IPaymentService
{
    Task<PaymentResponse> ProcessPaymentAsync(string orderId, decimal amount, string currency = "USD");
    Task<RefundResponse> RefundPaymentAsync(string paymentId, string orderId, string reason);
}

public record ProductResponse(string Id, string Name, string Description, decimal Price, int StockQuantity, string Category);
public record StockResponse(bool Success, string Message);
public record PaymentResponse(string PaymentId, string OrderId, string Status, string Message);
public record RefundResponse(bool Success, string Message);
