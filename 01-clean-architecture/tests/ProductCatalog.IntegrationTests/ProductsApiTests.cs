using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProductCatalog.Application.DTOs;
using ProductCatalog.Infrastructure.Data;

namespace ProductCatalog.IntegrationTests;

public class TestWebApplicationFactory : WebApplicationFactory<ProductCatalog.API.Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor != null) services.Remove(descriptor);

            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}"));
        });
    }
}

public class ProductsApiTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ProductsApiTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetProducts_Should_Return_Success()
    {
        var response = await _client.GetAsync("/api/products");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateProduct_Should_Return_Created()
    {
        var categoryResponse = await _client.PostAsJsonAsync("/api/categories", new CreateCategoryDto
        {
            Name = "Test Category",
            Description = "Test Description"
        });
        var category = await categoryResponse.Content.ReadFromJsonAsync<CategoryDto>();

        var dto = new CreateProductDto
        {
            Name = "Test Product",
            Description = "A test product",
            Price = 29.99m,
            CategoryId = category!.Id
        };

        var response = await _client.PostAsJsonAsync("/api/products", dto);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var product = await response.Content.ReadFromJsonAsync<ProductDto>();
        product!.Name.Should().Be("Test Product");
    }

    [Fact]
    public async Task GetProduct_By_Id_Should_Return_NotFound_For_Invalid_Id()
    {
        var response = await _client.GetAsync($"/api/products/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteProduct_Should_Return_NoContent()
    {
        var categoryResponse = await _client.PostAsJsonAsync("/api/categories", new CreateCategoryDto
        {
            Name = "Delete Category",
            Description = "To be deleted"
        });
        var category = await categoryResponse.Content.ReadFromJsonAsync<CategoryDto>();

        var createResponse = await _client.PostAsJsonAsync("/api/products", new CreateProductDto
        {
            Name = "Delete Product",
            Description = "Will be deleted",
            Price = 10m,
            CategoryId = category!.Id
        });
        var product = await createResponse.Content.ReadFromJsonAsync<ProductDto>();

        var deleteResponse = await _client.DeleteAsync($"/api/products/{product!.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await _client.GetAsync($"/api/products/{product.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
