namespace Shared.Contracts.Events;

public class PaymentProcessedEvent
{
    public string PaymentId { get; set; } = string.Empty;
    public string OrderId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime ProcessedAt { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
}
