namespace RemoteFunctions.Core.Application;

public sealed class RemoteFunctionExecutor
{
    private readonly IRemoteFunctionGateway _gateway;

    public RemoteFunctionExecutor(IRemoteFunctionGateway gateway)
    {
        ArgumentNullException.ThrowIfNull(gateway);
        _gateway = gateway;
    }

    public Task<RemoteFunctionResult<TResponse>> ExecuteAsync<TRequest, TResponse>(
        RemoteFunctionInvocation<TRequest> invocation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(invocation);
        return _gateway.InvokeAsync<TRequest, TResponse>(invocation, cancellationToken);
    }
}
