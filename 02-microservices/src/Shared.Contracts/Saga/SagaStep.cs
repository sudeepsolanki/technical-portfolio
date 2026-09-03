namespace Shared.Contracts.Saga;

public enum SagaStep
{
    None = 0,
    ReserveStock = 1,
    ProcessPayment = 2,
    CompleteOrder = 3,
    CompensatePayment = 4,
    CompensateStock = 5,
    Completed = 6,
    Failed = 7
}

public enum SagaStatus
{
    Pending = 0,
    InProgress = 1,
    Completed = 2,
    Failed = 3,
    Compensating = 4,
    Compensated = 5
}
