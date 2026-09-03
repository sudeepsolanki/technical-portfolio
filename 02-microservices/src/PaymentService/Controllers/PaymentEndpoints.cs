using Microsoft.EntityFrameworkCore;
using PaymentService.Data;
using Shared.Contracts.DTOs;

namespace PaymentService.Controllers;

public static class PaymentEndpoints
{
    public static void MapPaymentEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/payments")
            .WithTags("Payments");

        group.MapGet("/", async (PaymentDbContext context) =>
        {
            var payments = await context.Payments
                .OrderByDescending(p => p.ProcessedAt)
                .ToListAsync();
            return Results.Ok(payments.Select(p => new PaymentDto(
                p.Id.ToString(), p.OrderId, p.Amount, p.Currency, p.Status, p.ProcessedAt)));
        });

        group.MapGet("/{id}", async (string id, PaymentDbContext context) =>
        {
            if (!Guid.TryParse(id, out var guidId)) return Results.BadRequest();
            var payment = await context.Payments.FindAsync(guidId);
            if (payment == null) return Results.NotFound();
            return Results.Ok(new PaymentDto(
                payment.Id.ToString(), payment.OrderId, payment.Amount,
                payment.Currency, payment.Status, payment.ProcessedAt));
        });

        group.MapGet("/order/{orderId}", async (string orderId, PaymentDbContext context) =>
        {
            var payments = await context.Payments
                .Where(p => p.OrderId == orderId)
                .OrderByDescending(p => p.ProcessedAt)
                .ToListAsync();
            return Results.Ok(payments.Select(p => new PaymentDto(
                p.Id.ToString(), p.OrderId, p.Amount, p.Currency, p.Status, p.ProcessedAt)));
        });
    }
}
