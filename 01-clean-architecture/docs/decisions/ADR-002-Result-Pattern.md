# ADR-002: Result Pattern for Business Operations

## Status
Accepted

## Context
Application services need to communicate success or failure of operations to controllers without throwing exceptions for expected business rule violations.

## Decision
Implement a `Result<T>` pattern where services return `Result<T>.Success(value)` or `Result<T>.Failure(error)`.

## Consequences

### Positive
- Explicit error handling without exception overhead
- API controllers return appropriate HTTP status codes based on Result
- Unit tests can assert on result status
- No need for try-catch in business logic

### Negative
- Callers must check `IsSuccess` before accessing `Value`
- No stack trace for business rule violations

## Alternatives Considered
1. **Throw exceptions**: Creates noise in logs, harder to control flow
2. **Return null**: Loses error information
3. **Option/Maybe pattern**: More functional but less familiar to most C# developers
