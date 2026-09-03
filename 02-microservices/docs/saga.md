# Saga Pattern Implementation

## Overview
The Saga pattern manages distributed transactions across microservices. This project uses the Choreography-based saga pattern with MassTransit.

## Saga Flow

### Happy Path
```
CreateOrder → ReserveStock → ProcessPayment → CompleteOrder
```

### Compensation Path
```
CreateOrder → ReserveStock → ProcessPayment (FAIL) → CompensatePayment → ReleaseStock
```

## Event Flow

### 1. Order Creation
- Client POSTs to `/api/orders`
- OrderService creates order with status "Pending"
- Publishes `OrderCreatedEvent`

### 2. Stock Reservation
- ProductService receives `OrderCreatedEvent`
- Reserves stock for each item
- Publishes `StockReservedEvent` on success
- Publishes `StockReservationFailedEvent` on failure

### 3. Payment Processing
- PaymentService receives `OrderCreatedEvent`
- Processes payment
- Publishes `PaymentProcessedEvent` on success
- Publishes `PaymentFailedEvent` on failure

### 4. Order Completion
- OrderService receives `PaymentProcessedEvent`
- Updates order status to "Completed"

### 5. Compensation
- On payment failure: OrderService publishes `OrderCompensatedEvent`
- ProductService releases reserved stock

## Idempotency

Each consumer tracks processed message IDs to prevent duplicate processing:
```csharp
if (_idempotencyService.IsAlreadyProcessed(key)) return;
// ... process message
_idempotencyService.MarkAsProcessed(key);
```

## Saga Logs

The OrderService logs each saga step:
```json
{
  "orderId": "ord-abc123",
  "step": "ReserveStock",
  "status": "Completed",
  "message": "Stock reserved successfully",
  "timestamp": "2026-09-03T12:00:00Z"
}
```

## Monitoring

Access saga logs via:
```
GET /api/orders/{id}/saga
```
