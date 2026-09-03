# Resilience Patterns

## Idempotency

Every consumer checks for duplicate messages:
```csharp
if (_idempotencyService.IsAlreadyProcessed(messageId))
{
    _logger.LogWarning("Duplicate message: {Id}", messageId);
    return;
}
```

## Retry with MassTransit

MassTransit provides automatic retry:
```csharp
cfg.ConfigureEndpoints(context, e =>
{
    e.UseMessageRetry(r => r.Intervals(
        TimeSpan.FromSeconds(1),
        TimeSpan.FromSeconds(5),
        TimeSpan.FromSeconds(10)
    ));
});
```

## Circuit Breaker

For HTTP calls between services, consider adding:
```csharp
builder.Services.AddHttpClient<IProductService, ProductServiceHttpClient>()
    .AddPolicyHandler(Policy
        .Handle<HttpRequestException>()
        .CircuitBreakerAsync(
            handledEventsAllowedBeforeBreaking: 3,
            durationOfBreak: TimeSpan.FromSeconds(30)
        ));
```

## Outbox Pattern

For guaranteed message delivery, implement the outbox pattern:
1. Save entity and outbox message in same transaction
2. Background process publishes outbox messages
3. Mark as published after successful send

## Health Checks

Each service exposes health endpoints:
```csharp
app.MapGet("/health", () => Results.Ok(new { Status = "Healthy" }));
```

## Graceful Shutdown

MassTransit handles graceful shutdown automatically, completing in-flight messages before stopping.
