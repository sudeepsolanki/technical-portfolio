# Microservices Architecture Diagram

## Service Topology

```mermaid
graph TB
    subgraph Clients
        React[React Frontend :3000]
    end

    subgraph Gateway
        YARP[API Gateway :5000]
    end

    subgraph Services
        PS[Product Service :5001]
        OS[Order Service :5002]
        PmS[Payment Service :5003]
    end

    subgraph Databases[(Database Per Service)]
        DB1[(products.db)]
        DB2[(orders.db)]
        DB3[(payments.db)]
    end

    subgraph Messaging
        RMQ[RabbitMQ :5672]
        Mgmt[RabbitMQ Mgmt :15672]
    end

    React --> YARP
    YARP --> PS
    YARP --> OS
    YARP --> PmS

    PS --> DB1
    OS --> DB2
    PmS --> DB3

    OS -->|Publish| RMQ
    PS -->|Publish| RMQ
    PmS -->|Publish| RMQ
    RMQ -->|Consume| PS
    RMQ -->|Consume| OS
    RMQ -->|Consume| PmS
    RMQ --> Mgmt
```

## Saga Event Flow

```mermaid
sequenceDiagram
    box rgb(232, 245, 233) Order Service
    participant Client
    participant OS as OrderService
    end

    box rgb(227, 242, 253) Product Service
    participant PS as ProductService
    end

    box rgb(252, 228, 236) Payment Service
    participant PmS as PaymentService
    end

    box rgb(255, 243, 224) Messaging
    participant RMQ as RabbitMQ
    end

    Client->>OS: POST /api/orders
    OS->>OS: Create Order (Pending)
    OS->>RMQ: OrderCreatedEvent

    par Parallel Processing
        RMQ->>PS: OrderCreatedEvent
        PS->>PS: Reserve Stock
        PS->>RMQ: StockReservedEvent
    and
        RMQ->>PmS: OrderCreatedEvent
        PmS->>PmS: Process Payment
        PmS->>RMQ: PaymentProcessedEvent
    end

    RMQ->>OS: StockReservedEvent
    RMQ->>OS: PaymentProcessedEvent
    OS->>OS: Complete Order
```

## Compensation Flow

```mermaid
sequenceDiagram
    participant OS as OrderService
    participant PmS as PaymentService
    participant PS as ProductService
    participant RMQ as RabbitMQ

    Note over OS,RMQ: Payment Fails
    PmS->>RMQ: PaymentFailedEvent
    RMQ->>OS: PaymentFailedEvent
    OS->>OS: Mark Order Compensating
    OS->>RMQ: OrderCompensatedEvent

    par Compensation
        RMQ->>PmS: OrderCompensatedEvent
        PmS->>PmS: Refund Payment
    and
        RMQ->>PS: OrderCompensatedEvent
        PS->>PS: Release Stock
    end
```
