using MassTransit;
using Microsoft.EntityFrameworkCore;
using PaymentService.Data;
using Shared.Contracts.Events;
using Shared.Idempotency;
using System.Diagnostics.Metrics;

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
        var meter = new Meter("TechnicalPortfolio.PaymentService");
        var paymentsProcessed = meter.CreateCounter<long>(
            "payments_processed_total",
            description: "Total number of payments processed");

        var paymentsSuccessful = meter.CreateCounter<long>(
            "payments_successful_total",
            description: "Total number of successful payments");

        if (_idempotencyService.IsAlreadyProcessed($"payment-{message.OrderId}"))
        {
            _logger.LogWarning("Payment already processed for order {OrderId}", message.OrderId);
            return;
        }

        _logger.LogInformation("Processing payment for order {OrderId}, amount: {Amount}",
            message.OrderId, message.TotalAmount);
        paymentsProcessed.Add(1);
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
        paymentsSuccessful.Add(1);
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
        var meter = new Meter("TechnicalPortfolio.PaymentService");
        var message = context.Message;
        var paymentsFailed = meter.CreateCounter<long>(
            "payments_failed_total",
            description: "Total number of failed payments");
        if (message.CompensatedStep == "ProcessPayment")
        {
            _logger.LogInformation("Refunding payment for order {OrderId}", message.OrderId);

            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.OrderId == message.OrderId && p.Status == "Completed");

            if (payment != null)
            {

                payment.Status = "Refunded";
                await _context.SaveChangesAsync();
                paymentsFailed.Add(1);
                _logger.LogInformation("Payment refunded for order {OrderId}", message.OrderId);
            }
        }
    }
}
