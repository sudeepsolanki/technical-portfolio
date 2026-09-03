using FluentAssertions;
using Moq;
using ProductCatalog.Application.DTOs;
using ProductCatalog.Domain.Entities;
using ProductCatalog.Domain.Interfaces;
using ProductCatalog.Application.Services;
using Microsoft.Extensions.Logging;

namespace ProductCatalog.UnitTests;

public class ProductServiceTests
{
    private readonly Mock<IProductRepository> _productRepoMock = new();
    private readonly Mock<ICategoryRepository> _categoryRepoMock = new();
    private readonly Mock<ILogger<ProductService>> _loggerMock = new();
    private readonly ProductService _sut;

    public ProductServiceTests()
    {
        _sut = new ProductService(_productRepoMock.Object, _categoryRepoMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GetByIdAsync_Should_Return_Product_When_Found()
    {
        var productId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var product = new Product
        {
            Id = productId,
            Name = "Test Product",
            Description = "Description",
            Price = 29.99m,
            CategoryId = categoryId,
            Category = new Category { Id = categoryId, Name = "Electronics" },
            CreatedAt = DateTime.UtcNow
        };

        _productRepoMock.Setup(x => x.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        var result = await _sut.GetByIdAsync(productId);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Test Product");
        result.Value.CategoryName.Should().Be("Electronics");
    }

    [Fact]
    public async Task GetByIdAsync_Should_Return_Failure_When_Not_Found()
    {
        _productRepoMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        var result = await _sut.GetByIdAsync(Guid.NewGuid());

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task CreateAsync_Should_Return_Failure_When_Product_Name_Exists()
    {
        _productRepoMock.Setup(x => x.ExistsByNameAsync("Existing Product", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var dto = new CreateProductDto { Name = "Existing Product", Description = "Desc", Price = 10, CategoryId = Guid.NewGuid() };

        var result = await _sut.CreateAsync(dto);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("already exists");
    }

    [Fact]
    public async Task CreateAsync_Should_Return_Failure_When_Category_Not_Found()
    {
        var categoryId = Guid.NewGuid();
        _productRepoMock.Setup(x => x.ExistsByNameAsync("New Product", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _categoryRepoMock.Setup(x => x.GetByIdAsync(categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Category?)null);

        var dto = new CreateProductDto { Name = "New Product", Description = "Desc", Price = 10, CategoryId = categoryId };

        var result = await _sut.CreateAsync(dto);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Category");
    }

    [Fact]
    public async Task CreateAsync_Should_Create_Product_When_Valid()
    {
        var categoryId = Guid.NewGuid();
        var category = new Category { Id = categoryId, Name = "Electronics" };

        _productRepoMock.Setup(x => x.ExistsByNameAsync("New Product", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _categoryRepoMock.Setup(x => x.GetByIdAsync(categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);
        _productRepoMock.Setup(x => x.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product p, CancellationToken _) => p);

        var dto = new CreateProductDto { Name = "New Product", Description = "Desc", Price = 10, CategoryId = categoryId };

        var result = await _sut.CreateAsync(dto);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("New Product");
        result.Value.CategoryId.Should().Be(categoryId);
        result.Value.CategoryName.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteAsync_Should_Return_Failure_When_Not_Found()
    {
        _productRepoMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        var result = await _sut.DeleteAsync(Guid.NewGuid());

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_Should_Succeed_When_Product_Exists()
    {
        var product = new Product { Id = Guid.NewGuid(), Name = "Test" };
        _productRepoMock.Setup(x => x.GetByIdAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        var result = await _sut.DeleteAsync(product.Id);

        result.IsSuccess.Should().BeTrue();
        _productRepoMock.Verify(x => x.DeleteAsync(product, It.IsAny<CancellationToken>()), Times.Once);
    }
}
