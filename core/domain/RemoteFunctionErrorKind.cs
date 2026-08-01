namespace RemoteFunctions.Core.Domain;

public enum RemoteFunctionErrorKind
{
    Configuration,
    Authentication,
    Authorization,
    Transport,
    Timeout,
    RateLimit,
    Protocol,
    RemoteExecution
}
