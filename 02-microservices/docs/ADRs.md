# Architecture Decision Records

## ADR-001: Use MassTransit for Messaging

**Status:** Accepted

**Context:** Need inter-service communication.

**Decision:** Use MassTransit with RabbitMQ for async messaging.

**Consequences:**
+ Built-in retry, circuit breaker, idempotency
+ Consumer-based architecture
+ Easy to add new message types
- Additional dependency
- Learning curve

## ADR-002: SQLite for Database Per Service

**Status:** Accepted

**Context:** Each service needs its own database.

**Decision:** Use SQLite for simplicity and portability.

**Consequences:**
+ Zero configuration
+ Portable across platforms
+ Good for demos and development
- Not suitable for production at scale
- Limited concurrent write performance

## ADR-003: Choreography-Based Saga

**Status:** Accepted

**Context:** Need distributed transaction management.

**Decision:** Use choreography (events) over orchestration (central coordinator).

**Consequences:**
+ Loose coupling between services
+ Each service owns its logic
+ Easy to add new steps
- Harder to track overall flow
- No central view of saga state

## ADR-004: YARP for API Gateway

**Status:** Accepted

**Context:** Need a single entry point for clients.

**Decision:** Use YARP (Yet Another Reverse Proxy) for routing.

**Consequences:**
+ Microsoft-supported
+ High performance
+ Easy configuration
+ Health checks built-in
- Limited transformation capabilities
