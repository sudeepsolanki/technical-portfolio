using Microsoft.Extensions.DependencyInjection;
using Shared.Idempotency;
using Shared.Messaging;

namespace Shared;

public static class DependencyInjection
{
    public static IServiceCollection AddSharedServices(this IServiceCollection services)
    {
        services.AddSingleton<IIdempotencyService, IdempotencyService>();
        services.AddScoped<IMessageBus, MessageBus>();
        return services;
    }
}
