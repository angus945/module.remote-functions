using RemoteFunctions.Core.Application;
using RemoteFunctions.Core.Domain;
using RemoteFunctions.Core.Presentation;
using RemoteFunctions.GoogleAppsScript.Application;
using RemoteFunctions.GoogleAppsScript.Infrastructure;

namespace RemoteFunctions.GoogleAppsScript.Presentation;

public sealed class GoogleAppsScriptRemoteFunctionClient
{
    private static readonly HttpClient SharedHttpClient = new(new HttpClientHandler
    {
        AllowAutoRedirect = false
    })
    {
        Timeout = TimeSpan.FromSeconds(10)
    };

    private readonly RemoteFunctionClient _client;

    public GoogleAppsScriptRemoteFunctionClient(GoogleAppsScriptRemoteFunctionOptions options)
        : this(options, SharedHttpClient)
    {
    }

    public GoogleAppsScriptRemoteFunctionClient(
        GoogleAppsScriptRemoteFunctionOptions options,
        HttpClient httpClient)
    {
        var gateway = new GoogleAppsScriptWebAppGateway(options, httpClient);
        _client = new RemoteFunctionClient(new RemoteFunctionExecutor(gateway));
    }

    public Task<RemoteFunctionResult> InvokeAsync(
        IReadOnlyDictionary<string, object?> arguments,
        CancellationToken cancellationToken = default)
    {
        return _client.InvokeAsync(arguments, cancellationToken);
    }
}
