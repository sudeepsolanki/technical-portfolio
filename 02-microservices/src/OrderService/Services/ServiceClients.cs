using System.Text.Json;
using Shared.Contracts.Services;

namespace OrderService.Services;

public class ProductServiceHttpClient : IProductService
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public ProductServiceHttpClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ProductResponse?> GetProductAsync(string productId)
    {
        var response = await _httpClient.GetAsync($"/api/products/{productId}");
        if (!response.IsSuccessStatusCode) return null;
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<ProductResponse>(content, _jsonOptions);
    }

    public async Task<List<ProductResponse>> GetProductsAsync(int page = 0, int pageSize = 20)
    {
        var response = await _httpClient.GetAsync($"/api/products?page={page}&pageSize={pageSize}");
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<ProductResponse>>(content, _jsonOptions) ?? new();
    }

    public async Task<StockResponse> ReserveStockAsync(string productId, int quantity, string orderId)
    {
        var response = await _httpClient.PostAsJsonAsync($"/api/products/{productId}/reserve", new { Quantity = quantity, OrderId = orderId });
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<StockResponse>(content, _jsonOptions) ?? new StockResponse(false, "Failed to deserialize response");
    }

    public async Task<StockResponse> ReleaseStockAsync(string productId, int quantity, string orderId)
    {
        var response = await _httpClient.PostAsJsonAsync($"/api/products/{productId}/release", new { Quantity = quantity, OrderId = orderId });
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<StockResponse>(content, _jsonOptions) ?? new StockResponse(false, "Failed to deserialize response");
    }
}
