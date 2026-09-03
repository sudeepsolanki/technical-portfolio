using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderApi.Controllers;
using OrderApi.Data;
using OrderApi.DTOs;
using OrderApi.Models;

namespace OrderApi.Tests;

public class OrdersControllerTests : IDisposable
{
    private readonly OrderDbContext _context;
    private readonly OrdersController _controller;

    public OrdersControllerTests()
    {
        var options = new DbContextOptionsBuilder<OrderDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new OrderDbContext(options);
        _context.Database.EnsureCreated();
        _controller = new OrdersController(_context);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public async Task GetAllOrders_ReturnsOkWithOrders()
    {
        var result = await _controller.GetAllOrders(CancellationToken.None);

        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var orders = okResult.Value.Should().BeAssignableTo<List<OrderDto>>().Subject;
        orders.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetOrderById_ExistingId_ReturnsOkWithOrder()
    {
        var id = Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890");

        var result = await _controller.GetOrderById(id, CancellationToken.None);

        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var order = okResult.Value.Should().BeOfType<OrderDto>().Subject;
        order.CustomerName.Should().Be("John Doe");
        order.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetOrderById_NonExistingId_ReturnsNotFound()
    {
        var id = Guid.NewGuid();

        var result = await _controller.GetOrderById(id, CancellationToken.None);

        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task CreateOrder_ValidDto_ReturnsCreatedAtAction()
    {
        var dto = new CreateOrderDto
        {
            CustomerName = "New Customer",
            Items = new List<CreateOrderItemDto>
            {
                new() { ProductName = "Product A", Quantity = 2, UnitPrice = 10.00m },
                new() { ProductName = "Product B", Quantity = 1, UnitPrice = 25.00m }
            }
        };

        var result = await _controller.CreateOrder(dto, CancellationToken.None);

        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        var order = createdResult.Value.Should().BeOfType<OrderDto>().Subject;
        order.CustomerName.Should().Be("New Customer");
        order.TotalAmount.Should().Be(45.00m);
        order.Items.Should().HaveCount(2);
        order.Status.Should().Be(OrderStatus.Pending);
    }

    [Fact]
    public async Task CreateOrder_SetsCorrectTotalAmount()
    {
        var dto = new CreateOrderDto
        {
            CustomerName = "Test Customer",
            Items = new List<CreateOrderItemDto>
            {
                new() { ProductName = "Widget", Quantity = 3, UnitPrice = 9.99m }
            }
        };

        var result = await _controller.CreateOrder(dto, CancellationToken.None);

        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        var order = createdResult.Value.Should().BeOfType<OrderDto>().Subject;
        order.TotalAmount.Should().Be(29.97m);
    }

    [Fact]
    public async Task UpdateOrderStatus_ExistingOrder_ReturnsNoContent()
    {
        var id = Guid.Parse("c3d4e5f6-a7b8-9012-cdef-123456789012");
        var dto = new UpdateOrderStatusDto { Status = OrderStatus.Processing };

        var result = await _controller.UpdateOrderStatus(id, dto, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
        var order = await _context.Orders.FindAsync(id);
        order!.Status.Should().Be(OrderStatus.Processing);
    }

    [Fact]
    public async Task UpdateOrderStatus_NonExistingOrder_ReturnsNotFound()
    {
        var id = Guid.NewGuid();
        var dto = new UpdateOrderStatusDto { Status = OrderStatus.Shipped };

        var result = await _controller.UpdateOrderStatus(id, dto, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task DeleteOrder_ExistingOrder_ReturnsNoContent()
    {
        var id = Guid.Parse("c3d4e5f6-a7b8-9012-cdef-123456789012");

        var result = await _controller.DeleteOrder(id, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
        var order = await _context.Orders.FindAsync(id);
        order.Should().BeNull();
    }

    [Fact]
    public async Task DeleteOrder_NonExistingOrder_ReturnsNotFound()
    {
        var id = Guid.NewGuid();

        var result = await _controller.DeleteOrder(id, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }
}
