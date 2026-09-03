using System.Text.Json;
using Shared.Contracts.Services;

namespace OrderService.Services;

public class PaymentServiceHttpClient : IPaymentService
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public PaymentServiceHttpClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<PaymentResponse> ProcessPaymentAsync(string orderId, decimal amount, string currency = "USD")
    {
        var response = await _httpClient.PostAsJsonAsync("/api/payments", new { OrderId = orderId, Amount = amount, Currency = currency });
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<PaymentResponse>(content, _jsonOptions) ?? new PaymentResponse("", orderId, "Failed", "Deserialization failed");
    }

    public async Task<RefundResponse> RefundPaymentAsync(string paymentId, string orderId, string reason)
    {
        var response = await _httpClient.PostAsJsonAsync($"/api/payments/{paymentId}/refund", new { OrderId = orderId, Reason = reason });
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<RefundResponse>(content, _jsonOptions) ?? new RefundResponse(false, "Deserialization failed");
    }
}
