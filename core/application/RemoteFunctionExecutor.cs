using RemoteFunctions.Core.Domain;

namespace RemoteFunctions.Core.Application;

public sealed class RemoteFunctionExecutor
{
    private readonly IRemoteFunctionGateway _gateway;

    public RemoteFunctionExecutor(IRemoteFunctionGateway gateway)
    {
        _gateway = gateway;
    }

    public Task<RemoteFunctionResult> ExecuteAsync(
        RemoteFunctionCall call,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(call);
        return _gateway.InvokeAsync(call, cancellationToken);
    }
}
