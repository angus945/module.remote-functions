using RemoteFunctions.Core.Application;
using RemoteFunctions.Core.Presentation;
using RemoteFunctions.GoogleAppsScript.Infrastructure.Configuration;
using RemoteFunctions.GoogleAppsScript.Infrastructure.Gateway;

namespace RemoteFunctions.GoogleAppsScript.Presentation;

public static class GoogleAppsScriptClientFactory
{
    private static readonly HttpClient SharedHttpClient = new(new HttpClientHandler
    {
        AllowAutoRedirect = false
    })
    {
        Timeout = TimeSpan.FromSeconds(10)
    };

    public static IRemoteFunctionClient Create(GoogleAppsScriptOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return Create(options, SharedHttpClient);
    }

    /// <summary>
    /// Creates a Google Apps Script remote-function client.
    /// </summary>
    /// <param name="options">Google Apps Script endpoint and shared access token settings.</param>
    /// <param name="httpClient">
    /// The HTTP client used for transport. Its handler must disable automatic
    /// redirects because redirects are validated by this module.
    /// </param>
    public static IRemoteFunctionClient Create(
        GoogleAppsScriptOptions options,
        HttpClient httpClient)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(httpClient);

        var gateway = new GoogleAppsScriptWebAppGateway(options, httpClient);
        var executor = new RemoteFunctionExecutor(gateway);
        return new RemoteFunctionClient(executor);
    }
}
