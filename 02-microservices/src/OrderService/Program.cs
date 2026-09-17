using MassTransit;
using Microsoft.EntityFrameworkCore;
using OrderService.Consumers;
using OrderService.Controllers;
using OrderService.Data;
using OrderService.Saga;
using OrderService.Services;
using Shared;
using Shared.Contracts.Services;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OrderService.Observability;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<SagaMetrics>();
builder.Services.AddDbContext<OrderDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default")
        ?? "Data Source=orders.db"));

builder.Services.AddHttpClient<IProductService, ProductServiceHttpClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:ProductService"] ?? "http://localhost:5001");
});
builder.Services.AddHttpClient<IPaymentService, PaymentServiceHttpClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:PaymentService"] ?? "http://localhost:5003");
});

builder.Services.AddScoped<SagaOrchestrator>();
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<StockReservedConsumer>();
    x.AddConsumer<StockReservationFailedConsumer>();
    x.AddConsumer<PaymentProcessedConsumer>();
    x.AddConsumer<PaymentFailedConsumer>();
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration["RabbitMQ:Host"] ?? "localhost", "/", h =>
        {
            h.Username(builder.Configuration["RabbitMQ:Username"] ?? "guest");
            h.Password(builder.Configuration["RabbitMQ:Password"] ?? "guest");
        });
        cfg.ConfigureEndpoints(context);
    });
});

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource =>
    {
        resource.AddService("OrderService");
    })
    .WithMetrics(metrics =>
    {
        metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddMeter("TechnicalPortfolio.OrderService")
            .AddPrometheusExporter();
    })
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri("http://tempo:4317");
            });
    });

builder.Services.AddSharedServices();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

app.MapPrometheusScrapingEndpoint();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
    context.Database.EnsureCreated();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.MapOrderEndpoints();

app.Run();

public partial class Program { }
