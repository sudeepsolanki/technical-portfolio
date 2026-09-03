using System.ComponentModel.DataAnnotations;

namespace OrderApi.DTOs;

public class CreateOrderDto
{
    [Required]
    [MaxLength(200)]
    public string CustomerName { get; set; } = string.Empty;

    [Required]
    public List<CreateOrderItemDto> Items { get; set; } = new();
}
