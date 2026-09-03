using Microsoft.Extensions.Logging;
using ProductCatalog.Application.Common;
using ProductCatalog.Application.DTOs;
using ProductCatalog.Application.Interfaces;
using ProductCatalog.Domain.Entities;
using ProductCatalog.Domain.Interfaces;

namespace ProductCatalog.Application.Services;

public class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly ILogger<CategoryService> _logger;

    public CategoryService(ICategoryRepository categoryRepository, ILogger<CategoryService> logger)
    {
        _categoryRepository = categoryRepository;
        _logger = logger;
    }

    public async Task<Result<CategoryDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var category = await _categoryRepository.GetByIdAsync(id, cancellationToken);

        if (category is null)
        {
            _logger.LogWarning("Category with ID {CategoryId} not found", id);
            return Result<CategoryDto>.Failure($"Category with ID {id} not found");
        }

        return Result<CategoryDto>.Success(MapToDto(category));
    }

    public async Task<Result<IReadOnlyList<CategoryDto>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var categories = await _categoryRepository.GetAllAsync(cancellationToken);
        return Result<IReadOnlyList<CategoryDto>>.Success(categories.Select(MapToDto).ToList());
    }

    public async Task<Result<CategoryDto>> CreateAsync(CreateCategoryDto dto, CancellationToken cancellationToken = default)
    {
        var exists = await _categoryRepository.ExistsByNameAsync(dto.Name, cancellationToken: cancellationToken);
        if (exists)
        {
            return Result<CategoryDto>.Failure($"A category with name '{dto.Name}' already exists");
        }

        var category = new Category
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Description = dto.Description,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _categoryRepository.AddAsync(category, cancellationToken);

        _logger.LogInformation("Category {CategoryId} created with name {CategoryName}", created.Id, created.Name);

        return Result<CategoryDto>.Success(MapToDto(created));
    }

    public async Task<Result<CategoryDto>> UpdateAsync(Guid id, UpdateCategoryDto dto, CancellationToken cancellationToken = default)
    {
        var category = await _categoryRepository.GetByIdAsync(id, cancellationToken);
        if (category is null)
        {
            return Result<CategoryDto>.Failure($"Category with ID {id} not found");
        }

        var exists = await _categoryRepository.ExistsByNameAsync(dto.Name, id, cancellationToken);
        if (exists)
        {
            return Result<CategoryDto>.Failure($"A category with name '{dto.Name}' already exists");
        }

        category.Name = dto.Name;
        category.Description = dto.Description;
        category.UpdatedAt = DateTime.UtcNow;

        await _categoryRepository.UpdateAsync(category, cancellationToken);

        _logger.LogInformation("Category {CategoryId} updated", id);

        return Result<CategoryDto>.Success(MapToDto(category));
    }

    public async Task<Result<bool>> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var category = await _categoryRepository.GetByIdAsync(id, cancellationToken);
        if (category is null)
        {
            return Result<bool>.Failure($"Category with ID {id} not found");
        }

        await _categoryRepository.DeleteAsync(category, cancellationToken);

        _logger.LogInformation("Category {CategoryId} deleted", id);

        return Result<bool>.Success(true);
    }

    private static CategoryDto MapToDto(Category category) => new()
    {
        Id = category.Id,
        Name = category.Name,
        Description = category.Description
    };
}
