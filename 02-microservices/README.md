# Microservices with Saga Pattern

A production-style .NET 9 microservices architecture demonstrating the Saga orchestration pattern with RabbitMQ messaging and database-per-service isolation.

## Architecture

```
┌─────────────┐     ┌──────────────┐     ┌──────────────────┐
│   React     │────▶│  API Gateway │────▶│   Product Service │
│   Frontend  │     │  (YARP)      │     │   (REST + EventBus)│
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

## Tech Stack

- **Backend:** .NET 9, Minimal APIs, MassTransit, Entity Framework Core
- **Messaging:** RabbitMQ via MassTransit
- **Gateway:** YARP Reverse Proxy
- **Frontend:** React 19, TypeScript, Vite
- **Database:** SQLite (database-per-service)
- **Tests:** xUnit, FluentAssertions, Moq

## Features

- **Saga Pattern:** Choreography-based distributed transactions
- **Database Per Service:** Each service owns its data
- **Event-Driven:** Asynchronous messaging via RabbitMQ
- **Idempotency:** Duplicate message detection
- **Compensation:** Automatic rollback on failure
- **API Gateway:** Single entry point with YARP
- **Docker:** Full containerization support
- **React Frontend:** Product browsing and order creation

## Project Structure

```
04-dotnet-microservices-grpc-saga/
├── src/
│   ├── ApiGateway/          # YARP reverse proxy
│   ├── ProductService/      # Product catalog & inventory
│   ├── OrderService/        # Order management & saga orchestrator
│   ├── PaymentService/      # Payment processing
│   ├── Shared/              # Cross-cutting concerns
│   └── Shared.Contracts/    # Events, DTOs, interfaces
├── tests/
│   └── SagaTests/           # Saga pattern tests
├── frontend/                # React application
├── docs/                    # Architecture documentation
├── docker-compose.yml
└── Microservices.sln
```

## Quick Start

### Run with Docker
```bash
docker compose up -d
```

### Run Locally
```bash
# Start services in separate terminals
dotnet run --project src/ProductService
dotnet run --project src/OrderService
dotnet run --project src/PaymentService
dotnet run --project src/ApiGateway

# Start frontend
cd frontend && npm install && npm run dev
```

### Run Tests
```bash
dotnet test
```

## API Endpoints

### Product Service (5001)
- `GET /api/products` - List products
- `GET /api/products/{id}` - Get product
- `POST /api/products` - Create product
- `GET /api/products/{id}/stock` - Get stock info

### Order Service (5002)
- `GET /api/orders` - List orders
- `GET /api/orders/{id}` - Get order
- `POST /api/orders` - Create order
- `GET /api/orders/{id}/saga` - Get saga logs

### Payment Service (5003)
- `GET /api/payments` - List payments
- `GET /api/payments/{id}` - Get payment

### API Gateway (5000)
- `GET /health` - Health check
- Routes to all services

## Saga Flow

1. **Create Order** → Order created with "Pending" status
2. **Reserve Stock** → ProductService reserves inventory
3. **Process Payment** → PaymentService processes payment
4. **Complete Order** → Order marked as "Completed"

If payment fails:
1. **Compensate Payment** → Refund payment
2. **Release Stock** → Release reserved inventory
3. **Fail Order** → Order marked as "Failed"

## Documentation

- [Architecture](docs/architecture.md)
- [Saga Pattern](docs/saga.md)
- [Messaging](docs/messaging.md)
- [Resilience](docs/resilience.md)
- [ADRs](docs/ADRs.md)
