namespace RemoteFunctions.GoogleAppsScript.Application;

public sealed class GoogleAppsScriptRemoteFunctionOptions
{
    private const string PlaceholderApiToken = "REPLACE_WITH_PRIVATE_API_TOKEN";
    private const string PlaceholderDeploymentId = "REPLACE_WITH_DEPLOYMENT_ID";

    public GoogleAppsScriptRemoteFunctionOptions(
        string endpointUrl,
        string apiToken,
        string source = "RemoteFunction")
    {
        EndpointUrl = endpointUrl.Trim();
        ApiToken = apiToken.Trim();
        Source = string.IsNullOrWhiteSpace(source) ? "RemoteFunction" : source.Trim();
    }

    public string EndpointUrl { get; }

    public string ApiToken { get; }

    public string Source { get; }

    public bool IsConfigured
    {
        get
        {
            return Uri.TryCreate(EndpointUrl, UriKind.Absolute, out var uri)
                && uri.Scheme == Uri.UriSchemeHttps
                && !string.IsNullOrWhiteSpace(ApiToken)
                && ApiToken != PlaceholderApiToken
                && !EndpointUrl.Contains(PlaceholderDeploymentId, StringComparison.Ordinal);
        }
    }
}
