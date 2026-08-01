using RemoteFunctions.Core.Domain;
using RemoteFunctions.GoogleAppsScript.Infrastructure.Contracts;

namespace RemoteFunctions.GoogleAppsScript.Infrastructure.Errors;

internal static class GoogleAppsScriptErrorMapper
{
    public static RemoteFunctionError NotConfigured()
    {
        return new RemoteFunctionError(
            "NOT_CONFIGURED",
            "Google Apps Script endpoint URL or shared access token is not configured.",
            RemoteFunctionErrorKind.Configuration);
    }

    public static RemoteFunctionError HttpStatus(int statusCode)
    {
        return new RemoteFunctionError(
            $"HTTP_{statusCode}",
            $"Google Apps Script endpoint returned HTTP {statusCode}.",
            statusCode == 429 ? RemoteFunctionErrorKind.RateLimit : RemoteFunctionErrorKind.Transport,
            statusCode is 429 or >= 500);
    }

    public static RemoteFunctionError Timeout()
    {
        return new RemoteFunctionError(
            "TIMEOUT",
            "Google Apps Script request timed out.",
            RemoteFunctionErrorKind.Timeout,
            retryable: true);
    }

    public static RemoteFunctionError NetworkError()
    {
        return new RemoteFunctionError(
            "NETWORK_ERROR",
            "Google Apps Script request failed before a response was received.",
            RemoteFunctionErrorKind.Transport,
            retryable: true);
    }

    public static RemoteFunctionError InvalidResponse()
    {
        return new RemoteFunctionError(
            "INVALID_RESPONSE",
            "Google Apps Script returned a response that does not match the envelope contract.",
            RemoteFunctionErrorKind.Protocol);
    }

    public static RemoteFunctionError MissingRedirectLocation(int statusCode)
    {
        return new RemoteFunctionError(
            "REDIRECT_LOCATION_MISSING",
            $"Google Apps Script returned HTTP {statusCode} without a redirect location.",
            RemoteFunctionErrorKind.Protocol);
    }

    public static RemoteFunctionError InsecureRedirect(Uri redirectUri)
    {
        return new RemoteFunctionError(
            "INSECURE_REDIRECT",
            $"Google Apps Script redirect target is not HTTPS: {redirectUri}.",
            RemoteFunctionErrorKind.Protocol);
    }

    public static RemoteFunctionError TooManyRedirects()
    {
        return new RemoteFunctionError(
            "TOO_MANY_REDIRECTS",
            "Google Apps Script redirect limit was exceeded.",
            RemoteFunctionErrorKind.Protocol);
    }

    public static RemoteFunctionError RemoteError(GoogleAppsScriptErrorResponse error)
    {
        var kind = error.Code switch
        {
            "UNAUTHORIZED" => RemoteFunctionErrorKind.Authentication,
            "FORBIDDEN" => RemoteFunctionErrorKind.Authorization,
            "RATE_LIMITED" => RemoteFunctionErrorKind.RateLimit,
            _ => RemoteFunctionErrorKind.RemoteExecution
        };

        return new RemoteFunctionError(
            error.Code,
            error.Message,
            kind,
            error.Retryable);
    }
}
