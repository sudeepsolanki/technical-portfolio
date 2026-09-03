# ADR-001: Clean Architecture

## Status
Accepted

## Context
We need to build a Product Catalog API that is testable, maintainable, and follows SOLID principles. The system needs to support CRUD operations with business rules and data access.

## Decision
Implement Clean Architecture with four layers: Domain, Application, Infrastructure, API.

## Consequences

### Positive
- Business logic is testable in isolation
- Infrastructure concerns are isolated
- Easy to swap database providers
- Clear separation of concerns

### Negative
- More projects/assemblies to manage
- Slightly more boilerplate code
- Learning curve for team members unfamiliar with the pattern

## Alternatives Considered
1. **Traditional N-tier**: Simpler but mixes concerns
2. **Vertical Slice Architecture**: Good but may be overkill for small projects
3. **CQRS**: Powerful but adds complexity not needed here
