namespace Shared.Contracts.DTOs;

public record OrderDto(
    string Id,
    string CustomerId,
    List<OrderItemDto> Items,
    decimal TotalAmount,
    string Status,
    DateTime CreatedAt,
    DateTime? CompletedAt);

public record OrderItemDto(
    string ProductId,
    int Quantity,
    decimal UnitPrice);
