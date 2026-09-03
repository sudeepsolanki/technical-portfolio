# Microservices Architecture

## Overview
This project demonstrates a microservices architecture with Saga orchestration pattern, using RabbitMQ for messaging and HTTP for synchronous communication.

## Architecture Diagram

```
┌─────────────┐     ┌──────────────┐     ┌──────────────────┐
│   React     │────▶│  API Gateway │────▶│   Product Service │
│   Frontend  │     │  (YARP)      │     │   (REST + MassTransit)│
└─────────────┘     └──────┬───────┘     └────────┬─────────┘
                           │                      │
                    ┌──────▼───────┐     ┌────────▼─────────┐
                    │              │     │   Order Service   │
                    │              │     │   (Saga Orchestrator)│
                    │              │     └────────┬─────────┘
                    └──────┬───────┘              │
                           │             ┌────────▼─────────┐
                    ┌──────▼───────┐     │ Payment Service  │
                    │   RabbitMQ   │◀───▶│ (Consumer)       │
                    └──────────────┘     └──────────────────┘
```

## Services

### Product Service (port 5001)
- Manages product catalog and inventory
- REST API for CRUD operations
- MassTransit consumers for saga events
- SQLite database-per-service

### Order Service (port 5002)
- Orchestrates the order saga
- HTTP clients for product/payment services
- MassTransit consumers for events
- SQLite database-per-service

### Payment Service (port 5003)
- Processes payments
- MassTransit consumers for order events
- SQLite database-per-service

### API Gateway (port 5000)
- YARP reverse proxy
- Routes requests to appropriate services
- CORS support

## Saga Pattern

The order creation follows a three-step saga:
1. **ReserveStock** - Reserve inventory for order items
2. **ProcessPayment** - Process payment for the order
3. **CompleteOrder** - Mark order as completed

If any step fails, compensation occurs:
- Payment failure → Release stock
- Stock failure → No compensation needed (order just fails)

## Messaging

- **OrderCreatedEvent** - Published when order is created
- **StockReservedEvent** - Published when stock is reserved
- **StockReservationFailedEvent** - Published when stock reservation fails
- **PaymentProcessedEvent** - Published when payment succeeds
- **PaymentFailedEvent** - Published when payment fails
- **OrderCompensatedEvent** - Published during compensation
- **OrderCompletedEvent** - Published when saga completes

## Database Per Service

Each service has its own SQLite database:
- `products.db` - Product catalog
- `orders.db` - Orders and saga logs
- `payments.db` - Payment records

## Running

```bash
# Build and run with Docker
docker compose up -d

# Or run locally
dotnet run --project src/ProductService
dotnet run --project src/OrderService
dotnet run --project src/PaymentService
dotnet run --project src/ApiGateway
```
