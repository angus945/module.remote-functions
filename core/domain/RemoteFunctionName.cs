namespace RemoteFunctions.Core.Domain;

public readonly record struct RemoteFunctionName
{
    public RemoteFunctionName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Function name cannot be empty.", nameof(value));
        }

        Value = value.Trim();
    }

    public string Value { get; }

    public override string ToString()
    {
        return Value;
    }
}
