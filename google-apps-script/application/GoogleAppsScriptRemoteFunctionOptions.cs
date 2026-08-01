namespace RemoteFunctions.GoogleAppsScript.Application;

public sealed class GoogleAppsScriptRemoteFunctionOptions
{
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
            return EndpointUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(ApiToken)
                && !EndpointUrl.Contains(PlaceholderDeploymentId, StringComparison.Ordinal);
        }
    }
}
