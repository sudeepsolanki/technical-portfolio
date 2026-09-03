using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using OrderService.Saga;
using Shared.Contracts.DTOs;
using Shared.Contracts.Events;
using Shared.Messaging;

namespace OrderService.Controllers;

public static class OrderEndpoints
{
    public static void MapOrderEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/orders")
            .WithTags("Orders");

        group.MapGet("/", async (OrderDbContext context) =>
        {
            var orders = await context.Orders
                .Include(o => o.Items)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
            return Results.Ok(orders.Select(MapToDto));
        });

        group.MapGet("/{id}", async (string id, OrderDbContext context) =>
        {
            var order = await context.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == id);
            if (order == null) return Results.NotFound();
            return Results.Ok(MapToDto(order));
        });

        group.MapPost("/", async (CreateOrderRequest request, OrderDbContext context, SagaOrchestrator saga, IMessageBus messageBus) =>
        {
            var items = new List<(string productId, int quantity, decimal unitPrice)>();
            foreach (var item in request.Items)
            {
                items.Add((item.ProductId, item.Quantity, 0m));
            }

            var order = await saga.CreateOrderAsync(request.CustomerId, items);

            await messageBus.PublishAsync(new OrderCreatedEvent
            {
                OrderId = order.Id,
                CustomerId = order.CustomerId,
                Items = order.Items.Select(i => new OrderItemEvent
                {
                    ProductId = i.ProductId,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice
                }).ToList(),
                TotalAmount = order.TotalAmount,
                CreatedAt = order.CreatedAt,
                CorrelationId = order.CorrelationId
            });

            return Results.Created($"/api/orders/{order.Id}", MapToDto(order));
        });

        group.MapGet("/{id}/saga", async (string id, OrderDbContext context) =>
        {
            var logs = await context.SagaLogs
                .Where(l => l.OrderId == id)
                .OrderBy(l => l.Timestamp)
                .ToListAsync();
            return Results.Ok(logs.Select(l => new
            {
                l.Step,
                l.Status,
                l.Message,
                l.Timestamp
            }));
        });
    }

    private static OrderDto MapToDto(Order order) => new(
        order.Id,
        order.CustomerId,
        order.Items.Select(i => new OrderItemDto(i.ProductId, i.Quantity, i.UnitPrice)).ToList(),
        order.TotalAmount,
        order.Status,
        order.CreatedAt,
        order.CompletedAt);
}
