namespace Shared.Contracts.DTOs;

public record CreateOrderRequest(
    string CustomerId,
    List<CreateOrderItemRequest> Items);

public record CreateOrderItemRequest(
    string ProductId,
    int Quantity);
