using MassTransit;
using Microsoft.Extensions.Logging;

namespace Shared.Messaging;

public class MessageBus : IMessageBus
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<MessageBus> _logger;

    public MessageBus(IPublishEndpoint publishEndpoint, ILogger<MessageBus> logger)
    {
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task PublishAsync<T>(T message, string correlationId = "", CancellationToken cancellationToken = default) where T : class
    {
        _logger.LogInformation("Publishing message {MessageType} with CorrelationId: {CorrelationId}",
            typeof(T).Name, correlationId);

        var context = new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId
        };

        await _publishEndpoint.Publish(message, ctx =>
        {
            ctx.Headers.Set("CorrelationId", correlationId);
        }, cancellationToken);
    }
}

public interface IMessageBus
{
    Task PublishAsync<T>(T message, string correlationId = "", CancellationToken cancellationToken = default) where T : class;
}
