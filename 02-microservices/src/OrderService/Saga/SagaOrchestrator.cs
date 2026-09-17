using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using OrderService.Observability;
using Shared.Contracts.Events;
using System.Diagnostics.Metrics;

namespace OrderService.Saga;

public class SagaOrchestrator
{
    private readonly OrderDbContext _context;
    private readonly ILogger<SagaOrchestrator> _logger;
    private readonly SagaMetrics _metrics;

    public SagaOrchestrator(OrderDbContext context,SagaMetrics metrics, ILogger<SagaOrchestrator> logger)
    {
        _context = context;
        _logger = logger;
        _metrics = metrics;
    }

    public async Task<Order> CreateOrderAsync(string customerId, List<(string productId, int quantity, decimal unitPrice)> items)
    {
        try
{
        _metrics.Started();
        var meter = new Meter("TechnicalPortfolio.OrderService");
        var ordersCreated = meter.CreateCounter<long>(
        "orders_created_total",
        description: "Total number of orders created");
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
        ordersCreated.Add(1);
        _logger.LogInformation("Saga started for order {OrderId} with correlation {CorrelationId}",
            order.Id, correlationId);
        _metrics.Completed();
        return order;
        }
        catch (Exception ex)
        {
            _metrics.Failed();
            _logger.LogError(ex, "Saga failed during order creation");
            throw;
        }

    }
}
