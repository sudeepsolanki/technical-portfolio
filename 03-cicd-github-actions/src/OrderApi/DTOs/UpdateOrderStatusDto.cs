using System.ComponentModel.DataAnnotations;
using OrderApi.Models;

namespace OrderApi.DTOs;

public class UpdateOrderStatusDto
{
    [Required]
    public OrderStatus Status { get; set; }
}
