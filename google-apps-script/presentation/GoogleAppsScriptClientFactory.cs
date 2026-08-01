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
        return Create(options, SharedHttpClient);
    }

    public static IRemoteFunctionClient Create(
        GoogleAppsScriptOptions options,
        HttpClient httpClient)
    {
        var gateway = new GoogleAppsScriptWebAppGateway(options, httpClient);
        var executor = new RemoteFunctionExecutor(gateway);
        return new RemoteFunctionClient(executor);
    }
}
