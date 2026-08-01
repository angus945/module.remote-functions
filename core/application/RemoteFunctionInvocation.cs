using RemoteFunctions.Core.Domain;

namespace RemoteFunctions.Core.Application;

public sealed record RemoteFunctionInvocation<TRequest>(
    RemoteFunctionName FunctionName,
    TRequest Request);
