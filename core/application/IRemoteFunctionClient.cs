namespace RemoteFunctions.Core.Application;

public interface IRemoteFunctionClient
{
    Task<RemoteFunctionResult<TResponse>> InvokeAsync<TRequest, TResponse>(
        string functionName,
        TRequest request,
        CancellationToken cancellationToken = default);

    Task<RemoteFunctionResult<TResponse>> InvokeAsync<TResponse>(
        string functionName,
        CancellationToken cancellationToken = default);
}
