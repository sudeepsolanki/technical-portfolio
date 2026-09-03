using MassTransit;
using Microsoft.EntityFrameworkCore;
using ProductService.Data;
using Shared.Contracts.Events;
using Shared.Contracts.Services;

namespace ProductService.Consumers;

public class OrderCreatedConsumer : IConsumer<OrderCreatedEvent>
{
    private readonly ProductDbContext _context;
    private readonly IProductService _productService;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<OrderCreatedConsumer> _logger;

    public OrderCreatedConsumer(
        ProductDbContext context,
        IProductService productService,
        IPublishEndpoint publishEndpoint,
        ILogger<OrderCreatedConsumer> logger)
    {
        _context = context;
        _productService = productService;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<OrderCreatedEvent> context)
    {
        var message = context.Message;
        _logger.LogInformation("Received OrderCreated for order {OrderId}", message.OrderId);

        foreach (var item in message.Items)
        {
            var result = await _productService.ReserveStockAsync(item.ProductId, item.Quantity, message.OrderId);
            if (!result.Success)
            {
                _logger.LogWarning("Stock reservation failed for product {ProductId}: {Message}",
                    item.ProductId, result.Message);

                await _publishEndpoint.Publish(new StockReservationFailedEvent
                {
                    OrderId = message.OrderId,
                    ProductId = item.ProductId,
                    Reason = result.Message,
                    FailedAt = DateTime.UtcNow,
                    CorrelationId = message.CorrelationId
                });
                return;
            }
        }

        await _publishEndpoint.Publish(new StockReservedEvent
        {
            OrderId = message.OrderId,
            ProductId = message.Items.First().ProductId,
            Quantity = message.Items.Sum(i => i.Quantity),
            ReservedAt = DateTime.UtcNow,
            CorrelationId = message.CorrelationId
        });

        _logger.LogInformation("Stock reserved for order {OrderId}", message.OrderId);
    }
}

public class OrderCompensatedConsumer : IConsumer<OrderCompensatedEvent>
{
    private readonly ProductDbContext _context;
    private readonly IProductService _productService;
    private readonly ILogger<OrderCompensatedConsumer> _logger;

    public OrderCompensatedConsumer(
        ProductDbContext context,
        IProductService productService,
        ILogger<OrderCompensatedConsumer> logger)
    {
        _context = context;
        _productService = productService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<OrderCompensatedEvent> context)
    {
        var message = context.Message;
        _logger.LogInformation("Received OrderCompensated for order {OrderId}, step {Step}",
            message.OrderId, message.CompensatedStep);

        if (message.CompensatedStep == "ReserveStock")
        {
            var reservations = await _context.StockReservations
                .Where(r => r.OrderId == message.OrderId && r.IsActive)
                .ToListAsync();

            foreach (var reservation in reservations)
            {
                await _productService.ReleaseStockAsync(reservation.ProductId, reservation.Quantity, message.OrderId);
            }

            _logger.LogInformation("Stock released for compensated order {OrderId}", message.OrderId);
        }
    }
}
