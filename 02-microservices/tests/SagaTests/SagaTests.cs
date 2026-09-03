using FluentAssertions;
using Moq;
using Shared.Contracts.Events;
using Shared.Contracts.Saga;
using Shared.Idempotency;
using Shared.Messaging;

namespace SagaTests;

public class SagaTests
{
    private readonly Mock<IMessageBus> _messageBusMock;
    private readonly Mock<IIdempotencyService> _idempotencyServiceMock;

    public SagaTests()
    {
        _messageBusMock = new Mock<IMessageBus>();
        _idempotencyServiceMock = new Mock<IIdempotencyService>();
    }

    [Fact]
    public void OrderCreatedEvent_ShouldHaveRequiredFields()
    {
        var evt = new OrderCreatedEvent
        {
            OrderId = "ord-001",
            CustomerId = "cust-001",
            Items = new List<OrderItemEvent>
            {
                new() { ProductId = "prod-001", Quantity = 2, UnitPrice = 10.00m }
            },
            TotalAmount = 20.00m,
            CreatedAt = DateTime.UtcNow,
            CorrelationId = Guid.NewGuid().ToString()
        };

        evt.OrderId.Should().Be("ord-001");
        evt.CustomerId.Should().Be("cust-001");
        evt.Items.Should().HaveCount(1);
        evt.TotalAmount.Should().Be(20.00m);
        evt.CorrelationId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void StockReservedEvent_ShouldHaveRequiredFields()
    {
        var evt = new StockReservedEvent
        {
            OrderId = "ord-001",
            ProductId = "prod-001",
            Quantity = 2,
            ReservedAt = DateTime.UtcNow,
            CorrelationId = Guid.NewGuid().ToString()
        };

        evt.OrderId.Should().Be("ord-001");
        evt.ProductId.Should().Be("prod-001");
        evt.Quantity.Should().Be(2);
    }

    [Fact]
    public void PaymentProcessedEvent_ShouldHaveRequiredFields()
    {
        var evt = new PaymentProcessedEvent
        {
            PaymentId = Guid.NewGuid().ToString(),
            OrderId = "ord-001",
            Amount = 100.00m,
            Status = "Completed",
            ProcessedAt = DateTime.UtcNow,
            CorrelationId = Guid.NewGuid().ToString()
        };

        evt.PaymentId.Should().NotBeNullOrEmpty();
        evt.Status.Should().Be("Completed");
        evt.Amount.Should().Be(100.00m);
    }

    [Fact]
    public void OrderFailedEvent_ShouldCaptureFailureReason()
    {
        var evt = new OrderFailedEvent
        {
            OrderId = "ord-001",
            Reason = "Insufficient stock",
            FailedStep = "ReserveStock",
            FailedAt = DateTime.UtcNow,
            CorrelationId = Guid.NewGuid().ToString()
        };

        evt.Reason.Should().Be("Insufficient stock");
        evt.FailedStep.Should().Be("ReserveStock");
    }

    [Fact]
    public void OrderCompensatedEvent_ShouldTrackCompensation()
    {
        var evt = new OrderCompensatedEvent
        {
            OrderId = "ord-001",
            CompensatedStep = "ProcessPayment",
            Reason = "Payment failed",
            CompensatedAt = DateTime.UtcNow,
            CorrelationId = Guid.NewGuid().ToString()
        };

        evt.CompensatedStep.Should().Be("ProcessPayment");
    }

    [Fact]
    public void SagaStep_Enum_ShouldHaveAllSteps()
    {
        Enum.GetValues<SagaStep>().Should().Contain(new[]
        {
            SagaStep.None,
            SagaStep.ReserveStock,
            SagaStep.ProcessPayment,
            SagaStep.CompleteOrder,
            SagaStep.CompensatePayment,
            SagaStep.CompensateStock,
            SagaStep.Completed,
            SagaStep.Failed
        });
    }

    [Fact]
    public void SagaStatus_Enum_ShouldHaveAllStatuses()
    {
        Enum.GetValues<SagaStatus>().Should().Contain(new[]
        {
            SagaStatus.Pending,
            SagaStatus.InProgress,
            SagaStatus.Completed,
            SagaStatus.Failed,
            SagaStatus.Compensating,
            SagaStatus.Compensated
        });
    }

    [Fact]
    public void SagaState_ShouldTrackStepLogs()
    {
        var state = new SagaState
        {
            OrderId = "ord-001",
            CorrelationId = Guid.NewGuid().ToString(),
            CurrentStep = SagaStep.ReserveStock,
            Status = SagaStatus.InProgress,
            StepLogs = new List<SagaStepLog>
            {
                new() { Step = SagaStep.ReserveStock, Status = SagaStatus.InProgress, Timestamp = DateTime.UtcNow }
            }
        };

        state.StepLogs.Should().HaveCount(1);
        state.CurrentStep.Should().Be(SagaStep.ReserveStock);
    }

    [Fact]
    public void MessageBus_ShouldPublishEvents()
    {
        var orderEvent = new OrderCreatedEvent
        {
            OrderId = "ord-001",
            CustomerId = "cust-001",
            Items = new List<OrderItemEvent>(),
            TotalAmount = 50.00m,
            CorrelationId = Guid.NewGuid().ToString()
        };

        _messageBusMock.Setup(m => m.PublishAsync(It.IsAny<OrderCreatedEvent>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _messageBusMock.Object.PublishAsync(orderEvent, orderEvent.CorrelationId).Wait();

        _messageBusMock.Verify(m => m.PublishAsync(orderEvent, orderEvent.CorrelationId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void IdempotencyService_ShouldDetectDuplicates()
    {
        var service = new IdempotencyService(Mock.Of<Microsoft.Extensions.Logging.ILogger<IdempotencyService>>());
        var key = "test-key-001";

        service.IsAlreadyProcessed(key).Should().BeFalse();

        service.MarkAsProcessed(key);

        service.IsAlreadyProcessed(key).Should().BeTrue();
    }

    [Fact]
    public void IdempotencyService_ShouldAllowRemoval()
    {
        var service = new IdempotencyService(Mock.Of<Microsoft.Extensions.Logging.ILogger<IdempotencyService>>());
        var key = "test-key-002";

        service.MarkAsProcessed(key);
        service.Remove(key);

        service.IsAlreadyProcessed(key).Should().BeFalse();
    }

    [Fact]
    public void OrderCreatedEvent_ShouldSupportMultipleItems()
    {
        var evt = new OrderCreatedEvent
        {
            OrderId = "ord-multi",
            CustomerId = "cust-001",
            Items = new List<OrderItemEvent>
            {
                new() { ProductId = "prod-001", Quantity = 2, UnitPrice = 100m },
                new() { ProductId = "prod-002", Quantity = 3, UnitPrice = 50m },
                new() { ProductId = "prod-003", Quantity = 1, UnitPrice = 200m }
            },
            TotalAmount = 550m,
            CorrelationId = Guid.NewGuid().ToString()
        };

        evt.Items.Should().HaveCount(3);
        evt.Items.Sum(i => i.Quantity * i.UnitPrice).Should().Be(550m);
    }
}
