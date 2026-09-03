namespace Shared.Contracts.DTOs;

public record CreatePaymentRequest(
    string OrderId,
    decimal Amount,
    string Currency = "USD",
    string PaymentMethod = "credit_card");
