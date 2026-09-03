namespace Shared.Contracts.Events;

public class OrderFailedEvent
{
    public string OrderId { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string FailedStep { get; set; } = string.Empty;
    public DateTime FailedAt { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
}
