using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using Shared.Contracts.Events;

namespace OrderService.Saga;

public class SagaOrchestrator
{
    private readonly OrderDbContext _context;
    private readonly ILogger<SagaOrchestrator> _logger;

    public SagaOrchestrator(OrderDbContext context, ILogger<SagaOrchestrator> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Order> CreateOrderAsync(string customerId, List<(string productId, int quantity, decimal unitPrice)> items)
    {
        var correlationId = Guid.NewGuid().ToString();
        var order = new Order
        {
            Id = $"ord-{Guid.NewGuid().ToString()[..8]}",
            CustomerId = customerId,
            Status = "Pending",
            CorrelationId = correlationId,
            CreatedAt = DateTime.UtcNow,
            Items = items.Select(i => new OrderItem
            {
                Id = Guid.NewGuid(),
                ProductId = i.productId,
                Quantity = i.quantity,
                UnitPrice = i.unitPrice
            }).ToList()
        };

        order.TotalAmount = order.Items.Sum(i => i.Quantity * i.UnitPrice);
        _context.Orders.Add(order);

        _context.SagaLogs.Add(new SagaLog
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            Step = "CreateOrder",
            Status = "Started",
            Message = $"Order created with {items.Count} items",
            Timestamp = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        _logger.LogInformation("Saga started for order {OrderId} with correlation {CorrelationId}",
            order.Id, correlationId);

        return order;
    }
}
