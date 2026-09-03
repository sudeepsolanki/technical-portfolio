using MassTransit;
using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using Shared.Contracts.Events;
using Shared.Idempotency;

namespace OrderService.Consumers;

public class StockReservedConsumer : IConsumer<StockReservedEvent>
{
    private readonly OrderDbContext _context;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IIdempotencyService _idempotencyService;
    private readonly ILogger<StockReservedConsumer> _logger;

    public StockReservedConsumer(
        OrderDbContext context,
        IPublishEndpoint publishEndpoint,
        IIdempotencyService idempotencyService,
        ILogger<StockReservedConsumer> logger)
    {
        _context = context;
        _publishEndpoint = publishEndpoint;
        _idempotencyService = idempotencyService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<StockReservedEvent> context)
    {
        var message = context.Message;

        if (_idempotencyService.IsAlreadyProcessed($"stock-reserved-{message.OrderId}"))
        {
            _logger.LogWarning("Stock already reserved for order {OrderId}", message.OrderId);
            return;
        }

        _logger.LogInformation("Stock reserved for order {OrderId}", message.OrderId);

        var order = await _context.Orders.FindAsync(message.OrderId);
        if (order == null) return;

        order.Status = "StockReserved";
        order.UpdatedAt = DateTime.UtcNow;

        _context.SagaLogs.Add(new SagaLog
        {
            Id = Guid.NewGuid(),
            OrderId = message.OrderId,
            Step = "ReserveStock",
            Status = "Completed",
            Message = "Stock reserved successfully",
            Timestamp = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
        _idempotencyService.MarkAsProcessed($"stock-reserved-{message.OrderId}");

        await _publishEndpoint.Publish(new OrderCreatedEvent
        {
            OrderId = message.OrderId,
            CustomerId = order.CustomerId,
            Items = order.Items.Select(i => new OrderItemEvent
            {
                ProductId = i.ProductId,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice
            }).ToList(),
            TotalAmount = order.TotalAmount,
            CorrelationId = message.CorrelationId
        });
    }
}

public class StockReservationFailedConsumer : IConsumer<StockReservationFailedEvent>
{
    private readonly OrderDbContext _context;
    private readonly IIdempotencyService _idempotencyService;
    private readonly ILogger<StockReservationFailedConsumer> _logger;

    public StockReservationFailedConsumer(
        OrderDbContext context,
        IIdempotencyService idempotencyService,
        ILogger<StockReservationFailedConsumer> logger)
    {
        _context = context;
        _idempotencyService = idempotencyService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<StockReservationFailedEvent> context)
    {
        var message = context.Message;

        if (_idempotencyService.IsAlreadyProcessed($"stock-failed-{message.OrderId}"))
        {
            _logger.LogWarning("Stock failure already processed for order {OrderId}", message.OrderId);
            return;
        }

        _logger.LogWarning("Stock reservation failed for order {OrderId}: {Reason}",
            message.OrderId, message.Reason);

        var order = await _context.Orders.FindAsync(message.OrderId);
        if (order == null) return;

        order.Status = "Failed";
        order.UpdatedAt = DateTime.UtcNow;

        _context.SagaLogs.Add(new SagaLog
        {
            Id = Guid.NewGuid(),
            OrderId = message.OrderId,
            Step = "ReserveStock",
            Status = "Failed",
            Message = message.Reason,
            Timestamp = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
        _idempotencyService.MarkAsProcessed($"stock-failed-{message.OrderId}");
    }
}

public class PaymentProcessedConsumer : IConsumer<PaymentProcessedEvent>
{
    private readonly OrderDbContext _context;
    private readonly IIdempotencyService _idempotencyService;
    private readonly ILogger<PaymentProcessedConsumer> _logger;

    public PaymentProcessedConsumer(
        OrderDbContext context,
        IIdempotencyService idempotencyService,
        ILogger<PaymentProcessedConsumer> logger)
    {
        _context = context;
        _idempotencyService = idempotencyService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<PaymentProcessedEvent> context)
    {
        var message = context.Message;

        if (_idempotencyService.IsAlreadyProcessed($"payment-processed-{message.OrderId}"))
        {
            _logger.LogWarning("Payment already processed for order {OrderId}", message.OrderId);
            return;
        }

        _logger.LogInformation("Payment processed for order {OrderId}", message.OrderId);

        var order = await _context.Orders.FindAsync(message.OrderId);
        if (order == null) return;

        order.Status = "Completed";
        order.PaymentId = message.PaymentId;
        order.CompletedAt = DateTime.UtcNow;
        order.UpdatedAt = DateTime.UtcNow;

        _context.SagaLogs.Add(new SagaLog
        {
            Id = Guid.NewGuid(),
            OrderId = message.OrderId,
            Step = "ProcessPayment",
            Status = "Completed",
            Message = $"Payment {message.PaymentId} processed",
            Timestamp = DateTime.UtcNow
        });

        _context.SagaLogs.Add(new SagaLog
        {
            Id = Guid.NewGuid(),
            OrderId = message.OrderId,
            Step = "CompleteOrder",
            Status = "Completed",
            Message = "Order completed successfully",
            Timestamp = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
        _idempotencyService.MarkAsProcessed($"payment-processed-{message.OrderId}");
    }
}

public class PaymentFailedConsumer : IConsumer<PaymentFailedEvent>
{
    private readonly OrderDbContext _context;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IIdempotencyService _idempotencyService;
    private readonly ILogger<PaymentFailedConsumer> _logger;

    public PaymentFailedConsumer(
        OrderDbContext context,
        IPublishEndpoint publishEndpoint,
        IIdempotencyService idempotencyService,
        ILogger<PaymentFailedConsumer> logger)
    {
        _context = context;
        _publishEndpoint = publishEndpoint;
        _idempotencyService = idempotencyService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<PaymentFailedEvent> context)
    {
        var message = context.Message;

        if (_idempotencyService.IsAlreadyProcessed($"payment-failed-{message.OrderId}"))
        {
            _logger.LogWarning("Payment failure already processed for order {OrderId}", message.OrderId);
            return;
        }

        _logger.LogWarning("Payment failed for order {OrderId}: {Reason}",
            message.OrderId, message.Reason);

        var order = await _context.Orders.FindAsync(message.OrderId);
        if (order == null) return;

        order.Status = "Compensating";
        order.UpdatedAt = DateTime.UtcNow;

        _context.SagaLogs.Add(new SagaLog
        {
            Id = Guid.NewGuid(),
            OrderId = message.OrderId,
            Step = "ProcessPayment",
            Status = "Failed",
            Message = message.Reason,
            Timestamp = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
        _idempotencyService.MarkAsProcessed($"payment-failed-{message.OrderId}");

        await _publishEndpoint.Publish(new OrderCompensatedEvent
        {
            OrderId = message.OrderId,
            CompensatedStep = "ProcessPayment",
            Reason = message.Reason,
            CompensatedAt = DateTime.UtcNow,
            CorrelationId = message.CorrelationId
        });
    }
}
