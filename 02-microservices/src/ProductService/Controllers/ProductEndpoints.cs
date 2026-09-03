using Microsoft.EntityFrameworkCore;
using ProductService.Data;
using Shared.Contracts.DTOs;

namespace ProductService.Controllers;

public static class ProductEndpoints
{
    public static void MapProductEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/products")
            .WithTags("Products");

        group.MapGet("/", async (ProductDbContext context) =>
        {
            var products = await context.Products.ToListAsync();
            return Results.Ok(products.Select(p => new ProductDto(p.Id, p.Name, p.Description, p.Price, p.StockQuantity, p.Category)));
        });

        group.MapGet("/{id}", async (string id, ProductDbContext context) =>
        {
            var product = await context.Products.FindAsync(id);
            if (product == null) return Results.NotFound();
            return Results.Ok(new ProductDto(product.Id, product.Name, product.Description, product.Price, product.StockQuantity, product.Category));
        });

        group.MapPost("/", async (CreateProductRequest request, ProductDbContext context) =>
        {
            var product = new Product
            {
                Id = $"prod-{Guid.NewGuid().ToString()[..8]}",
                Name = request.Name,
                Description = request.Description,
                Price = request.Price,
                StockQuantity = request.StockQuantity,
                Category = request.Category
            };

            context.Products.Add(product);
            await context.SaveChangesAsync();

            return Results.Created($"/api/products/{product.Id}",
                new ProductDto(product.Id, product.Name, product.Description, product.Price, product.StockQuantity, product.Category));
        });

        group.MapPut("/{id}", async (string id, UpdateProductRequest request, ProductDbContext context) =>
        {
            var product = await context.Products.FindAsync(id);
            if (product == null) return Results.NotFound();

            product.Name = request.Name;
            product.Description = request.Description;
            product.Price = request.Price;
            product.StockQuantity = request.StockQuantity;
            product.Category = request.Category;

            await context.SaveChangesAsync();
            return Results.Ok(new ProductDto(product.Id, product.Name, product.Description, product.Price, product.StockQuantity, product.Category));
        });

        group.MapDelete("/{id}", async (string id, ProductDbContext context) =>
        {
            var product = await context.Products.FindAsync(id);
            if (product == null) return Results.NotFound();

            context.Products.Remove(product);
            await context.SaveChangesAsync();
            return Results.NoContent();
        });

        group.MapGet("/stock/{id}", async (string id, ProductDbContext context) =>
        {
            var product = await context.Products.FindAsync(id);
            if (product == null) return Results.NotFound();
            return Results.Ok(new StockInfo(product.Id, product.StockQuantity));
        });
    }
}

public record CreateProductRequest(string Name, string Description, decimal Price, int StockQuantity, string Category);
public record UpdateProductRequest(string Name, string Description, decimal Price, int StockQuantity, string Category);
public record StockInfo(string ProductId, int Quantity);
