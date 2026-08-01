namespace RemoteFunctions.Core.Application;

public sealed class EmptyRemoteFunctionRequest
{
    public static EmptyRemoteFunctionRequest Instance { get; } = new();

    private EmptyRemoteFunctionRequest()
    {
    }
}
