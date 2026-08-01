namespace RemoteFunctions.GoogleAppsScript.Infrastructure;

internal sealed record GoogleAppsScriptWebAppResponse(
    bool Ok,
    string? RecordId,
    bool Duplicate,
    string? Error);
