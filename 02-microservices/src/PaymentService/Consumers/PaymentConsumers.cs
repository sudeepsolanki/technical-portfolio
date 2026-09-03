using MassTransit;
using Microsoft.EntityFrameworkCore;
using PaymentService.Data;
using Shared.Contracts.Events;
using Shared.Idempotency;

namespace PaymentService.Consumers;

public class OrderCreatedConsumer : IConsumer<OrderCreatedEvent>
{
    private readonly PaymentDbContext _context;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IIdempotencyService _idempotencyService;
    private readonly ILogger<OrderCreatedConsumer> _logger;

    public OrderCreatedConsumer(
        PaymentDbContext context,
        IPublishEndpoint publishEndpoint,
        IIdempotencyService idempotencyService,
        ILogger<OrderCreatedConsumer> logger)
    {
        _context = context;
        _publishEndpoint = publishEndpoint;
        _idempotencyService = idempotencyService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<OrderCreatedEvent> context)
    {
        var message = context.Message;

        if (_idempotencyService.IsAlreadyProcessed($"payment-{message.OrderId}"))
        {
            _logger.LogWarning("Payment already processed for order {OrderId}", message.OrderId);
            return;
        }

        _logger.LogInformation("Processing payment for order {OrderId}, amount: {Amount}",
            message.OrderId, message.TotalAmount);

        var payment = new PaymentRecord
        {
            Id = Guid.NewGuid(),
            OrderId = message.OrderId,
            Amount = message.TotalAmount,
            Currency = "USD",
            PaymentMethod = "credit_card",
            Status = "Completed",
            ProcessedAt = DateTime.UtcNow
        };

        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();

        _idempotencyService.MarkAsProcessed($"payment-{message.OrderId}");

        await _publishEndpoint.Publish(new PaymentProcessedEvent
        {
            PaymentId = payment.Id.ToString(),
            OrderId = message.OrderId,
            Amount = payment.Amount,
            Status = "Completed",
            ProcessedAt = DateTime.UtcNow,
            CorrelationId = message.CorrelationId
        });

        _logger.LogInformation("Payment completed for order {OrderId}", message.OrderId);
    }
}

public class OrderCompensatedConsumer : IConsumer<OrderCompensatedEvent>
{
    private readonly PaymentDbContext _context;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IIdempotencyService _idempotencyService;
    private readonly ILogger<OrderCompensatedConsumer> _logger;

    public OrderCompensatedConsumer(
        PaymentDbContext context,
        IPublishEndpoint publishEndpoint,
        IIdempotencyService idempotencyService,
        ILogger<OrderCompensatedConsumer> logger)
    {
        _context = context;
        _publishEndpoint = publishEndpoint;
        _idempotencyService = idempotencyService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<OrderCompensatedEvent> context)
    {
        var message = context.Message;

        if (message.CompensatedStep == "ProcessPayment")
        {
            _logger.LogInformation("Refunding payment for order {OrderId}", message.OrderId);

            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.OrderId == message.OrderId && p.Status == "Completed");

            if (payment != null)
            {
                payment.Status = "Refunded";
                await _context.SaveChangesAsync();
                _logger.LogInformation("Payment refunded for order {OrderId}", message.OrderId);
            }
        }
    }
}
