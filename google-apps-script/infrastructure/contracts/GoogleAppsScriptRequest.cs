namespace RemoteFunctions.GoogleAppsScript.Infrastructure.Contracts;

internal sealed record GoogleAppsScriptRequest(
    string Function,
    object? Payload,
    string Source,
    string Token);
