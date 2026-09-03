namespace Shared.Contracts.Events;

public class StockReservedEvent
{
    public string OrderId { get; set; } = string.Empty;
    public string ProductId { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public DateTime ReservedAt { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
}
