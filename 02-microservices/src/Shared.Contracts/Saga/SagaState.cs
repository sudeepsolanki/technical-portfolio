namespace Shared.Contracts.Saga;

public class SagaState
{
    public string OrderId { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    public SagaStep CurrentStep { get; set; }
    public SagaStatus Status { get; set; }
    public string? PaymentId { get; set; }
    public string? FailureReason { get; set; }
    public int RetryCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<SagaStepLog> StepLogs { get; set; } = new();
}

public class SagaStepLog
{
    public string OrderId { get; set; } = string.Empty;
    public SagaStep Step { get; set; }
    public SagaStatus Status { get; set; }
    public string? Message { get; set; }
    public DateTime Timestamp { get; set; }
}
