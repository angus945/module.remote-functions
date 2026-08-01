using RemoteFunctions.Core.Domain;

namespace RemoteFunctions.Core.Application;

public sealed record RemoteFunctionInvocation<TRequest>
{
    public RemoteFunctionInvocation(RemoteFunctionName functionName, TRequest request)
    {
        ArgumentNullException.ThrowIfNull(functionName);
        ArgumentNullException.ThrowIfNull(request);

        FunctionName = functionName;
        Request = request;
    }

    public RemoteFunctionName FunctionName { get; }

    public TRequest Request { get; }
}
