namespace RemoteFunctions.Core.Domain;

public sealed record RemoteFunctionError
{
    public RemoteFunctionError(
        string code,
        string message,
        RemoteFunctionErrorKind kind,
        bool retryable = false)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Error code cannot be empty.", nameof(code));
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("Error message cannot be empty.", nameof(message));
        }

        Code = code.Trim();
        Message = message.Trim();
        Kind = kind;
        Retryable = retryable;
    }

    public string Code { get; }

    public string Message { get; }

    public RemoteFunctionErrorKind Kind { get; }

    public bool Retryable { get; }

    public override string ToString()
    {
        return Code;
    }
}
