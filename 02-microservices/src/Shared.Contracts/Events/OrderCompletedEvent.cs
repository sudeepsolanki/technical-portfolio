namespace Shared.Contracts.Events;

public class OrderCompletedEvent
{
    public string OrderId { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public string PaymentId { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public DateTime CompletedAt { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
}
