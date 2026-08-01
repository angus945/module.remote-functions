using RemoteFunctions.Core.Application;
using RemoteFunctions.Core.Domain;

namespace RemoteFunctions.Core.Presentation;

public sealed class RemoteFunctionClient : IRemoteFunctionClient
{
    private readonly RemoteFunctionExecutor _executor;

    public RemoteFunctionClient(RemoteFunctionExecutor executor)
    {
        _executor = executor;
    }

    public Task<RemoteFunctionResult<TResponse>> InvokeAsync<TRequest, TResponse>(
        string functionName,
        TRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var invocation = new RemoteFunctionInvocation<TRequest>(
            new RemoteFunctionName(functionName),
            request);
        return _executor.ExecuteAsync<TRequest, TResponse>(invocation, cancellationToken);
    }

    public Task<RemoteFunctionResult<TResponse>> InvokeAsync<TResponse>(
        string functionName,
        CancellationToken cancellationToken = default)
    {
        var invocation = new RemoteFunctionInvocation<EmptyRemoteFunctionRequest>(
            new RemoteFunctionName(functionName),
            EmptyRemoteFunctionRequest.Instance);
        return _executor.ExecuteAsync<EmptyRemoteFunctionRequest, TResponse>(
            invocation,
            cancellationToken);
    }
}
