using System.Net;
using System.Net.Http.Json;
using CustomerApi.Data;
using CustomerApi.DTOs;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CustomerApi.Tests;

public class CustomersControllerTests : IClassFixture<TestWebApplicationFactory>, IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly TestWebApplicationFactory _factory;

    public CustomersControllerTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CustomerDbContext>();
        await db.Customers.ExecuteDeleteAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private static CreateCustomerDto CreateValidCustomer(string? email = null) => new()
    {
        FirstName = "John",
        LastName = "Doe",
        Email = email ?? $"test_{Guid.NewGuid():N}@example.com",
        Phone = "+1234567890"
    };

    [Fact]
    public async Task GetCustomers_ReturnsEmptyList_Initially()
    {
        var response = await _client.GetAsync("/api/customers");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var customers = await response.Content.ReadFromJsonAsync<List<CustomerDto>>();
        customers.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public async Task PostCustomer_CreatesCustomer_ReturnsCreated()
    {
        var dto = CreateValidCustomer();

        var response = await _client.PostAsJsonAsync("/api/customers", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var customer = await response.Content.ReadFromJsonAsync<CustomerDto>();
        customer.Should().NotBeNull();
        customer!.FirstName.Should().Be(dto.FirstName);
        customer.Email.Should().Be(dto.Email);
        customer.Id.Should().NotBeEmpty();
        customer.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task GetCustomer_ById_ReturnsCustomer()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/customers", CreateValidCustomer());
        var created = await createResponse.Content.ReadFromJsonAsync<CustomerDto>();

        var response = await _client.GetAsync($"/api/customers/{created!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var customer = await response.Content.ReadFromJsonAsync<CustomerDto>();
        customer!.Id.Should().Be(created.Id);
        customer.FirstName.Should().Be("John");
    }

    [Fact]
    public async Task GetCustomer_InvalidId_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"/api/customers/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PutCustomer_UpdatesCustomer_ReturnsOk()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/customers", CreateValidCustomer());
        var created = await createResponse.Content.ReadFromJsonAsync<CustomerDto>();

        var updateDto = new UpdateCustomerDto
        {
            FirstName = "Jane",
            LastName = "Smith",
            Email = $"jane_{Guid.NewGuid():N}@example.com",
            Phone = "+0987654321",
            IsActive = true
        };

        var response = await _client.PutAsJsonAsync($"/api/customers/{created!.Id}", updateDto);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<CustomerDto>();
        updated!.FirstName.Should().Be("Jane");
        updated.Email.Should().Be(updateDto.Email);
    }

    [Fact]
    public async Task DeleteCustomer_SoftDeletes_ReturnsNoContent()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/customers", CreateValidCustomer());
        var created = await createResponse.Content.ReadFromJsonAsync<CustomerDto>();

        var response = await _client.DeleteAsync($"/api/customers/{created!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await _client.GetAsync($"/api/customers/{created.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PostCustomer_DuplicateEmail_ReturnsConflict()
    {
        var email = $"dupe_{Guid.NewGuid():N}@example.com";
        var dto = CreateValidCustomer(email);
        await _client.PostAsJsonAsync("/api/customers", dto);

        var duplicate = CreateValidCustomer(email);
        var response = await _client.PostAsJsonAsync("/api/customers", duplicate);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task DeleteCustomer_SoftDeletedNotInList()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/customers", CreateValidCustomer());
        var created = await createResponse.Content.ReadFromJsonAsync<CustomerDto>();

        await _client.DeleteAsync($"/api/customers/{created!.Id}");

        var listResponse = await _client.GetAsync("/api/customers");
        var customers = await listResponse.Content.ReadFromJsonAsync<List<CustomerDto>>();

        customers.Should().NotContain(c => c.Id == created.Id);
    }
}
