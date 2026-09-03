namespace Shared.Contracts.DTOs;

public record ProductDto(
    string Id,
    string Name,
    string Description,
    decimal Price,
    int StockQuantity,
    string Category);
