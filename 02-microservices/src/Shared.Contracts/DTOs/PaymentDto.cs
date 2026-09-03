namespace Shared.Contracts.DTOs;

public record PaymentDto(
    string Id,
    string OrderId,
    decimal Amount,
    string Currency,
    string Status,
    DateTime ProcessedAt);
