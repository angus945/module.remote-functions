namespace RemoteFunctions.GoogleAppsScript.Infrastructure.Configuration;

public sealed class GoogleAppsScriptOptions
{
    public GoogleAppsScriptOptions(
        string endpointUrl,
        string sharedAccessToken,
        string source = "RemoteFunction")
    {
        ArgumentNullException.ThrowIfNull(endpointUrl);
        ArgumentNullException.ThrowIfNull(sharedAccessToken);

        EndpointUrl = endpointUrl.Trim();
        SharedAccessToken = sharedAccessToken.Trim();
        Source = string.IsNullOrWhiteSpace(source) ? "RemoteFunction" : source.Trim();
    }

    public string EndpointUrl { get; }

    public string SharedAccessToken { get; }

    public string Source { get; }

    public bool IsConfigured
    {
        get
        {
            return Uri.TryCreate(EndpointUrl, UriKind.Absolute, out var uri)
                && uri.Scheme == Uri.UriSchemeHttps
                && !string.IsNullOrWhiteSpace(SharedAccessToken);
        }
    }

    internal Uri? EndpointUri
    {
        get
        {
            return Uri.TryCreate(EndpointUrl, UriKind.Absolute, out var uri)
                && uri.Scheme == Uri.UriSchemeHttps
                ? uri
                : null;
        }
    }
}
