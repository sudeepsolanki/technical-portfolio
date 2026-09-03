# ADR-002: SQLite for Development/Testing

## Status
Accepted

## Context
The API needs a database for development and testing. SQL Server requires more setup and licensing. For a portfolio project demonstrating CI/CD concepts, we need something lightweight and CI-friendly.

## Decision
Use SQLite for local development and CI/CD pipelines. Use SQL Server or PostgreSQL for production.

## Consequences

### Positive
- Zero configuration for CI/CD
- No external dependencies
- Fast test execution
- File-based database (easy to inspect)

### Negative
- Not suitable for production workloads
- Limited concurrency support
- Different behavior from production database

## Alternatives Considered
1. **SQL Server LocalDB**: Requires Windows, more setup
2. **PostgreSQL in Docker**: Better production parity but heavier for CI
3. **Testcontainers**: Best practice but adds complexity
