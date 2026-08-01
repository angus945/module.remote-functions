using System.Net;
using System.Text;
using System.Text.Json;
using RemoteFunctions.Core.Application;
using RemoteFunctions.GoogleAppsScript.Infrastructure.Configuration;
using RemoteFunctions.GoogleAppsScript.Infrastructure.Contracts;
using RemoteFunctions.GoogleAppsScript.Infrastructure.Errors;

namespace RemoteFunctions.GoogleAppsScript.Infrastructure.Gateway;

internal sealed class GoogleAppsScriptWebAppGateway : IRemoteFunctionGateway
{
    private const int MaxRedirects = 3;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly GoogleAppsScriptOptions _options;
    private readonly HttpClient _httpClient;

    public GoogleAppsScriptWebAppGateway(
        GoogleAppsScriptOptions options,
        HttpClient httpClient)
    {
        _options = options;
        _httpClient = httpClient;
    }

    public async Task<RemoteFunctionResult<TResponse>> InvokeAsync<TRequest, TResponse>(
        RemoteFunctionInvocation<TRequest> invocation,
        CancellationToken cancellationToken = default)
    {
        if (!_options.IsConfigured || _options.EndpointUri is null)
        {
            return RemoteFunctionResult<TResponse>.Failure(GoogleAppsScriptErrorMapper.NotConfigured());
        }

        var requestJson = CreateRequestJson(invocation);
        var requestUri = _options.EndpointUri;
        var method = HttpMethod.Post;
        var redirectCount = 0;

        try
        {
            while (true)
            {
                using var request = CreateHttpRequest(method, requestUri, requestJson);
                using var response = await _httpClient.SendAsync(request, cancellationToken);

                if (!IsRedirect(response.StatusCode))
                {
                    return await ReadApiResponseAsync<TResponse>(response, cancellationToken);
                }

                if (redirectCount >= MaxRedirects)
                {
                    return RemoteFunctionResult<TResponse>.Failure(GoogleAppsScriptErrorMapper.TooManyRedirects());
                }

                var redirectUri = ResolveRedirectUri(requestUri, response.Headers.Location);
                if (redirectUri is null)
                {
                    return RemoteFunctionResult<TResponse>.Failure(
                        GoogleAppsScriptErrorMapper.MissingRedirectLocation((int)response.StatusCode));
                }

                if (redirectUri.Scheme != Uri.UriSchemeHttps)
                {
                    return RemoteFunctionResult<TResponse>.Failure(
                        GoogleAppsScriptErrorMapper.InsecureRedirect(redirectUri));
                }

                method = RedirectMethod(response.StatusCode, method);
                requestUri = redirectUri;
                redirectCount++;
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return RemoteFunctionResult<TResponse>.Failure(GoogleAppsScriptErrorMapper.Timeout());
        }
        catch (HttpRequestException)
        {
            return RemoteFunctionResult<TResponse>.Failure(GoogleAppsScriptErrorMapper.NetworkError());
        }
        catch (JsonException)
        {
            return RemoteFunctionResult<TResponse>.Failure(GoogleAppsScriptErrorMapper.InvalidResponse());
        }
    }

    private string CreateRequestJson<TRequest>(RemoteFunctionInvocation<TRequest> invocation)
    {
        object? payload = invocation.Request is EmptyRemoteFunctionRequest ? null : invocation.Request;
        var request = new GoogleAppsScriptRequest(
            invocation.FunctionName.Value,
            payload,
            _options.Source,
            _options.SharedAccessToken);

        return JsonSerializer.Serialize(request, JsonOptions);
    }

    private static HttpRequestMessage CreateHttpRequest(
        HttpMethod method,
        Uri requestUri,
        string requestJson)
    {
        var request = new HttpRequestMessage(method, requestUri);
        if (method != HttpMethod.Get)
        {
            request.Content = new StringContent(requestJson, Encoding.UTF8, "application/json");
        }

        return request;
    }

    private static async Task<RemoteFunctionResult<TResponse>> ReadApiResponseAsync<TResponse>(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
        {
            return RemoteFunctionResult<TResponse>.Failure(
                GoogleAppsScriptErrorMapper.HttpStatus((int)response.StatusCode));
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var apiResponse = await JsonSerializer.DeserializeAsync<GoogleAppsScriptResponse<TResponse>>(
            stream,
            JsonOptions,
            cancellationToken);

        if (apiResponse is null)
        {
            return RemoteFunctionResult<TResponse>.Failure(GoogleAppsScriptErrorMapper.InvalidResponse());
        }

        if (apiResponse.Success)
        {
            return apiResponse.Data is null
                ? RemoteFunctionResult<TResponse>.Failure(GoogleAppsScriptErrorMapper.InvalidResponse())
                : RemoteFunctionResult<TResponse>.Success(apiResponse.Data);
        }

        return apiResponse.Error is null
            ? RemoteFunctionResult<TResponse>.Failure(GoogleAppsScriptErrorMapper.InvalidResponse())
            : RemoteFunctionResult<TResponse>.Failure(GoogleAppsScriptErrorMapper.RemoteError(apiResponse.Error));
    }

    private static Uri? ResolveRedirectUri(Uri requestUri, Uri? redirectUri)
    {
        if (redirectUri is null)
        {
            return null;
        }

        return redirectUri.IsAbsoluteUri ? redirectUri : new Uri(requestUri, redirectUri);
    }

    private static bool IsRedirect(HttpStatusCode statusCode)
    {
        return statusCode is HttpStatusCode.Moved
            or HttpStatusCode.Found
            or HttpStatusCode.SeeOther
            or HttpStatusCode.TemporaryRedirect
            or HttpStatusCode.PermanentRedirect;
    }

    private static HttpMethod RedirectMethod(HttpStatusCode statusCode, HttpMethod originalMethod)
    {
        return statusCode is HttpStatusCode.Moved
            or HttpStatusCode.Found
            or HttpStatusCode.SeeOther
            ? HttpMethod.Get
            : originalMethod;
    }
}
