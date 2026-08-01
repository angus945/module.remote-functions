using RemoteFunctions.Core.Application;
using RemoteFunctions.Core.Domain;

namespace RemoteFunctions.Core.Presentation;

public sealed class RemoteFunctionClient
{
    private readonly RemoteFunctionExecutor _executor;

    public RemoteFunctionClient(RemoteFunctionExecutor executor)
    {
        _executor = executor;
    }

    public Task<RemoteFunctionResult> InvokeAsync(
        IReadOnlyDictionary<string, object?> arguments,
        CancellationToken cancellationToken = default)
    {
        var call = RemoteFunctionCall.From(arguments);
        return _executor.ExecuteAsync(call, cancellationToken);
    }
}
