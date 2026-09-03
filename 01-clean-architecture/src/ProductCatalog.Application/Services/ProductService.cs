using Microsoft.Extensions.Logging;
using ProductCatalog.Application.Common;
using ProductCatalog.Application.DTOs;
using ProductCatalog.Application.Interfaces;
using ProductCatalog.Domain.Entities;
using ProductCatalog.Domain.Interfaces;

namespace ProductCatalog.Application.Services;

public class ProductService : IProductService
{
    private readonly IProductRepository _productRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly ILogger<ProductService> _logger;

    public ProductService(
        IProductRepository productRepository,
        ICategoryRepository categoryRepository,
        ILogger<ProductService> logger)
    {
        _productRepository = productRepository;
        _categoryRepository = categoryRepository;
        _logger = logger;
    }

    public async Task<Result<ProductDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var product = await _productRepository.GetByIdAsync(id, cancellationToken);

        if (product is null)
        {
            _logger.LogWarning("Product with ID {ProductId} not found", id);
            return Result<ProductDto>.Failure($"Product with ID {id} not found");
        }

        return Result<ProductDto>.Success(MapToDto(product));
    }

    public async Task<Result<PagedResult<ProductDto>>> GetAllAsync(ProductQueryParameters parameters, CancellationToken cancellationToken = default)
    {
        var products = await _productRepository.GetAllAsync(cancellationToken);

        if (parameters.CategoryId.HasValue)
        {
            products = products.Where(p => p.CategoryId == parameters.CategoryId.Value).ToList();
        }

        if (!string.IsNullOrWhiteSpace(parameters.Search))
        {
            var searchLower = parameters.Search.ToLower();
            products = products.Where(p =>
                p.Name.ToLower().Contains(searchLower) ||
                p.Description.ToLower().Contains(searchLower)).ToList();
        }

        var totalCount = products.Count;

        products = parameters.SortBy?.ToLower() switch
        {
            "name" => parameters.SortDescending
                ? products.OrderByDescending(p => p.Name).ToList()
                : products.OrderBy(p => p.Name).ToList(),
            "price" => parameters.SortDescending
                ? products.OrderByDescending(p => p.Price).ToList()
                : products.OrderBy(p => p.Price).ToList(),
            "createdat" => parameters.SortDescending
                ? products.OrderByDescending(p => p.CreatedAt).ToList()
                : products.OrderBy(p => p.CreatedAt).ToList(),
            _ => products.OrderBy(p => p.Name).ToList()
        };

        var pagedProducts = products
            .Skip((parameters.Page - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .ToList();

        var result = new PagedResult<ProductDto>
        {
            Items = pagedProducts.Select(MapToDto).ToList(),
            Page = parameters.Page,
            PageSize = parameters.PageSize,
            TotalCount = totalCount
        };

        return Result<PagedResult<ProductDto>>.Success(result);
    }

    public async Task<Result<ProductDto>> CreateAsync(CreateProductDto dto, CancellationToken cancellationToken = default)
    {
        var exists = await _productRepository.ExistsByNameAsync(dto.Name, cancellationToken: cancellationToken);
        if (exists)
        {
            return Result<ProductDto>.Failure($"A product with name '{dto.Name}' already exists");
        }

        var category = await _categoryRepository.GetByIdAsync(dto.CategoryId, cancellationToken);
        if (category is null)
        {
            return Result<ProductDto>.Failure($"Category with ID {dto.CategoryId} not found");
        }

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Description = dto.Description,
            Price = dto.Price,
            CategoryId = dto.CategoryId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _productRepository.AddAsync(product, cancellationToken);

        _logger.LogInformation("Product {ProductId} created with name {ProductName}", created.Id, created.Name);

        return Result<ProductDto>.Success(MapToDto(created));
    }

    public async Task<Result<ProductDto>> UpdateAsync(Guid id, UpdateProductDto dto, CancellationToken cancellationToken = default)
    {
        var product = await _productRepository.GetByIdAsync(id, cancellationToken);
        if (product is null)
        {
            return Result<ProductDto>.Failure($"Product with ID {id} not found");
        }

        var exists = await _productRepository.ExistsByNameAsync(dto.Name, id, cancellationToken);
        if (exists)
        {
            return Result<ProductDto>.Failure($"A product with name '{dto.Name}' already exists");
        }

        var category = await _categoryRepository.GetByIdAsync(dto.CategoryId, cancellationToken);
        if (category is null)
        {
            return Result<ProductDto>.Failure($"Category with ID {dto.CategoryId} not found");
        }

        product.Name = dto.Name;
        product.Description = dto.Description;
        product.Price = dto.Price;
        product.CategoryId = dto.CategoryId;
        product.IsActive = dto.IsActive;
        product.UpdatedAt = DateTime.UtcNow;

        await _productRepository.UpdateAsync(product, cancellationToken);

        _logger.LogInformation("Product {ProductId} updated", id);

        return Result<ProductDto>.Success(MapToDto(product));
    }

    public async Task<Result<bool>> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var product = await _productRepository.GetByIdAsync(id, cancellationToken);
        if (product is null)
        {
            return Result<bool>.Failure($"Product with ID {id} not found");
        }

        await _productRepository.DeleteAsync(product, cancellationToken);

        _logger.LogInformation("Product {ProductId} deleted", id);

        return Result<bool>.Success(true);
    }

    private static ProductDto MapToDto(Product product) => new()
    {
        Id = product.Id,
        Name = product.Name,
        Description = product.Description,
        Price = product.Price,
        CategoryId = product.CategoryId,
        CategoryName = product.Category?.Name ?? string.Empty,
        IsActive = product.IsActive,
        CreatedAt = product.CreatedAt,
        UpdatedAt = product.UpdatedAt
    };
}
