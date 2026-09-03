namespace Shared.Contracts.Events;

public class OrderCompensatedEvent
{
    public string OrderId { get; set; } = string.Empty;
    public string CompensatedStep { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public DateTime CompensatedAt { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
}
