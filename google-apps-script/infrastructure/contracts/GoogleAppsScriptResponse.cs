namespace RemoteFunctions.GoogleAppsScript.Infrastructure.Contracts;

internal sealed record GoogleAppsScriptResponse<T>(
    bool Success,
    T? Data,
    GoogleAppsScriptErrorResponse? Error);

internal sealed record GoogleAppsScriptErrorResponse(
    string Code,
    string Message,
    bool Retryable);
