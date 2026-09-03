# RabbitMQ Messaging

## Setup
RabbitMQ runs in Docker:
```bash
docker compose up rabbitmq
```

Management UI: http://localhost:15672 (guest/guest)

## Exchanges and Queues

MassTransit auto-configures:
- Exchange: `{namespace}.input` for each consumer
- Queues: One per consumer
- Retry queues for failed messages

## Message Flow

```
OrderCreatedEvent
  ├── ProductService (OrderCreatedConsumer)
  └── PaymentService (OrderCreatedConsumer)

StockReservedEvent
  └── OrderService (StockReservedConsumer)

StockReservationFailedEvent
  └── OrderService (StockReservationFailedConsumer)

PaymentProcessedEvent
  └── OrderService (PaymentProcessedConsumer)

PaymentFailedEvent
  └── OrderService (PaymentFailedConsumer)

OrderCompensatedEvent
  ├── ProductService (OrderCompensatedConsumer)
  └── PaymentService (OrderCompensatedConsumer)
```

## Configuration

Each service connects via:
```json
{
  "RabbitMQ": {
    "Host": "localhost",
    "Username": "guest",
    "Password": "guest"
  }
}
```

## MassTransit Configuration

```csharp
x.UsingRabbitMq((context, cfg) =>
{
    cfg.Host(host, "/", h =>
    {
        h.Username(username);
        h.Password(password);
    });
    cfg.ConfigureEndpoints(context);
});
```

## Monitoring

- Management UI: http://localhost:15672
- Check queue depths for backpressure
- Monitor message rates
