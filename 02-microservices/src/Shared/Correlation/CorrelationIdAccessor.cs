namespace Shared.Correlation;

public static class CorrelationIdAccessor
{
    private static readonly AsyncLocal<string?> _correlationId = new();

    public static string Current
    {
        get => _correlationId.Value ?? Guid.NewGuid().ToString();
        set => _correlationId.Value = value;
    }

    public static string Generate() => Guid.NewGuid().ToString();
}
