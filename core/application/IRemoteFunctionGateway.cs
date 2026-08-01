namespace RemoteFunctions.Core.Application;

public interface IRemoteFunctionGateway
{
    Task<RemoteFunctionResult<TResponse>> InvokeAsync<TRequest, TResponse>(
        RemoteFunctionInvocation<TRequest> invocation,
        CancellationToken cancellationToken = default);
}
