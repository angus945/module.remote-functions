using System.Net;
using System.Text;
using System.Text.Json;
using RemoteFunctions.Core.Application;
using RemoteFunctions.Core.Domain;
using RemoteFunctions.GoogleAppsScript.Application;
using RemoteFunctions.GoogleAppsScript.Domain;

namespace RemoteFunctions.GoogleAppsScript.Infrastructure;

internal sealed class GoogleAppsScriptWebAppGateway : IRemoteFunctionGateway
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly GoogleAppsScriptRemoteFunctionOptions _options;
    private readonly HttpClient _httpClient;

    public GoogleAppsScriptWebAppGateway(
        GoogleAppsScriptRemoteFunctionOptions options,
        HttpClient httpClient)
    {
        _options = options;
        _httpClient = httpClient;
    }

    public async Task<RemoteFunctionResult> InvokeAsync(
        RemoteFunctionCall call,
        CancellationToken cancellationToken = default)
    {
        if (!_options.IsConfigured)
        {
            return RemoteFunctionResult.Failed(GoogleAppsScriptErrorCodes.NotConfigured);
        }

        try
        {
            var request = CreateRequest(call);
            var json = JsonSerializer.Serialize(request, JsonOptions);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var response = await _httpClient.PostAsync(_options.EndpointUrl, content, cancellationToken);
            if (IsRedirect(response.StatusCode))
            {
                var redirectUri = ResolveRedirectUri(response.Headers.Location);
                if (redirectUri is null)
                {
                    return RemoteFunctionResult.Failed(GoogleAppsScriptErrorCodes.HttpStatus((int)response.StatusCode));
                }

                using var redirectResponse = await _httpClient.GetAsync(redirectUri, cancellationToken);
                return await ReadApiResponseAsync(redirectResponse, cancellationToken);
            }

            return await ReadApiResponseAsync(response, cancellationToken);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return RemoteFunctionResult.Failed(GoogleAppsScriptErrorCodes.Timeout);
        }
        catch (HttpRequestException)
        {
            return RemoteFunctionResult.Failed(GoogleAppsScriptErrorCodes.NetworkError);
        }
        catch (JsonException)
        {
            return RemoteFunctionResult.Failed(GoogleAppsScriptErrorCodes.InvalidResponse);
        }
    }

    private Dictionary<string, object?> CreateRequest(RemoteFunctionCall call)
    {
        var request = new Dictionary<string, object?>(call.Arguments, StringComparer.Ordinal)
        {
            ["token"] = _options.ApiToken,
            ["source"] = _options.Source
        };

        return request;
    }

    private async Task<RemoteFunctionResult> ReadApiResponseAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
        {
            return RemoteFunctionResult.Failed(GoogleAppsScriptErrorCodes.HttpStatus((int)response.StatusCode));
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var apiResponse = await JsonSerializer.DeserializeAsync<GoogleAppsScriptWebAppResponse>(
            stream,
            JsonOptions,
            cancellationToken);

        if (apiResponse?.Ok == true)
        {
            var data = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["duplicate"] = apiResponse.Duplicate
            };

            if (!string.IsNullOrWhiteSpace(apiResponse.RecordId))
            {
                data["recordId"] = apiResponse.RecordId;
            }

            return RemoteFunctionResult.Success(data);
        }

        return RemoteFunctionResult.Failed(apiResponse?.Error ?? GoogleAppsScriptErrorCodes.InvalidResponse);
    }

    private Uri? ResolveRedirectUri(Uri? redirectUri)
    {
        if (redirectUri is null)
        {
            return null;
        }

        if (redirectUri.IsAbsoluteUri)
        {
            return redirectUri;
        }

        return Uri.TryCreate(_options.EndpointUrl, UriKind.Absolute, out var endpointUri)
            ? new Uri(endpointUri, redirectUri)
            : null;
    }

    private static bool IsRedirect(HttpStatusCode statusCode)
    {
        return statusCode is HttpStatusCode.Moved
            or HttpStatusCode.Found
            or HttpStatusCode.SeeOther
            or HttpStatusCode.TemporaryRedirect
            or HttpStatusCode.PermanentRedirect;
    }
}
