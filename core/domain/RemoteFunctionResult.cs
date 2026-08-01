namespace RemoteFunctions.Core.Domain;

public sealed record RemoteFunctionResult(
    bool IsSuccess,
    string? Error,
    IReadOnlyDictionary<string, object?> Data)
{
    private static readonly IReadOnlyDictionary<string, object?> EmptyData =
        new Dictionary<string, object?>(StringComparer.Ordinal);

    public static RemoteFunctionResult Success(IReadOnlyDictionary<string, object?>? data = null)
    {
        return new RemoteFunctionResult(true, null, CopyData(data));
    }

    public static RemoteFunctionResult Failed(string error)
    {
        if (string.IsNullOrWhiteSpace(error))
        {
            throw new ArgumentException("A failed remote function result requires an error code.", nameof(error));
        }

        return new RemoteFunctionResult(false, error.Trim(), EmptyData);
    }

    private static IReadOnlyDictionary<string, object?> CopyData(IReadOnlyDictionary<string, object?>? data)
    {
        return data is null
            ? EmptyData
            : new Dictionary<string, object?>(data, StringComparer.Ordinal);
    }
}
