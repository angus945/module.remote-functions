namespace RemoteFunctions.Core.Domain;

public sealed class RemoteFunctionCall
{
    private readonly IReadOnlyDictionary<string, object?> _arguments;

    public RemoteFunctionCall(IEnumerable<KeyValuePair<string, object?>> arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);

        var normalizedArguments = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var argument in arguments)
        {
            var name = argument.Key?.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Remote function argument names cannot be empty.", nameof(arguments));
            }

            normalizedArguments[name] = argument.Value;
        }

        if (normalizedArguments.Count == 0)
        {
            throw new ArgumentException("A remote function call requires at least one argument.", nameof(arguments));
        }

        _arguments = normalizedArguments;
    }

    public IReadOnlyDictionary<string, object?> Arguments => _arguments;

    public static RemoteFunctionCall From(IReadOnlyDictionary<string, object?> arguments)
    {
        return new RemoteFunctionCall(arguments);
    }
}
