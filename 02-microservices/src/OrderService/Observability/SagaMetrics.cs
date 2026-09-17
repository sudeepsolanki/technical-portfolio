using System.Diagnostics.Metrics;

namespace OrderService.Observability;

public class SagaMetrics
{
    private readonly Counter<long> _started;
    private readonly Counter<long> _completed;
    private readonly Counter<long> _failed;
    private readonly Counter<long> _compensated;

    public SagaMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create("TechnicalPortfolio.OrderService");

        _started = meter.CreateCounter<long>(
            "order_saga_started_total",
            description: "Number of order sagas started");

        _completed = meter.CreateCounter<long>(
            "order_saga_completed_total",
            description: "Number of order sagas completed");

        _failed = meter.CreateCounter<long>(
            "order_saga_failed_total",
            description: "Number of order sagas failed");

        _compensated = meter.CreateCounter<long>(
            "order_saga_compensated_total",
            description: "Number of order sagas compensated");
    }

    public void Started() => _started.Add(1);

    public void Completed() => _completed.Add(1);

    public void Failed() => _failed.Add(1);

    public void Compensated() => _compensated.Add(1);
}

