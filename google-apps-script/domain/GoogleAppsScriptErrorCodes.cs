namespace RemoteFunctions.GoogleAppsScript.Domain;

public static class GoogleAppsScriptErrorCodes
{
    public const string NotConfigured = "NOT_CONFIGURED";
    public const string Timeout = "TIMEOUT";
    public const string NetworkError = "NETWORK_ERROR";
    public const string InvalidResponse = "INVALID_RESPONSE";

    public static string HttpStatus(int statusCode)
    {
        return $"HTTP_{statusCode}";
    }
}
