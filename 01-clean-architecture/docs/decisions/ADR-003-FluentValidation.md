# ADR-003: FluentValidation for Input Validation

## Status
Accepted

## Context
API endpoints receive DTOs that need validation before processing. Validation rules must be testable and maintainable.

## Decision
Use FluentValidation library for validating DTOs in the Application layer.

## Consequences

### Positive
- Fluent, readable validation rules
- Separated from DTOs (Single Responsibility)
- Easy to unit test
- Can be composed and combined
- Integrates with ASP.NET Core validation pipeline

### Negative
- Additional NuGet dependency
- Slightly more setup than Data Annotations

## Alternatives Considered
1. **Data Annotations**: Simpler but mixes validation with DTOs
2. **Manual validation**: More control but repetitive and error-prone
3. **Guard clauses**: Not reusable across DTOs
