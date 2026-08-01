using RemoteFunctions.Core.Domain;

namespace RemoteFunctions.Core.Application;

public interface IRemoteFunctionGateway
{
    Task<RemoteFunctionResult> InvokeAsync(
        RemoteFunctionCall call,
        CancellationToken cancellationToken = default);
}
