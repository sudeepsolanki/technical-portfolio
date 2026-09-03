using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Shared.Idempotency;

public class IdempotencyService : IIdempotencyService
{
    private readonly ConcurrentDictionary<string, bool> _processedKeys = new();
    private readonly ILogger<IdempotencyService> _logger;

    public IdempotencyService(ILogger<IdempotencyService> logger)
    {
        _logger = logger;
    }

    public bool IsAlreadyProcessed(string key)
    {
        var result = _processedKeys.ContainsKey(key);
        if (result)
        {
            _logger.LogWarning("Duplicate message detected with key: {Key}", key);
        }
        return result;
    }

    public void MarkAsProcessed(string key)
    {
        _processedKeys.TryAdd(key, true);
        _logger.LogDebug("Marked message as processed: {Key}", key);
    }

    public void Remove(string key)
    {
        _processedKeys.TryRemove(key, out _);
    }
}

public interface IIdempotencyService
{
    bool IsAlreadyProcessed(string key);
    void MarkAsProcessed(string key);
    void Remove(string key);
}
