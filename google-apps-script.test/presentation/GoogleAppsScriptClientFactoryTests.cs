using System.Net;
using System.Text.Json;
using RemoteFunctions.Core.Domain;
using RemoteFunctions.GoogleAppsScript.Infrastructure.Configuration;
using RemoteFunctions.GoogleAppsScript.Presentation;

namespace RemoteFunctions.GoogleAppsScript.Tests.Presentation;

public sealed class GoogleAppsScriptClientFactoryTests
{
    [Fact]
    public void GoogleAppsScriptOptions_NullEndpointThrowsArgumentNullException()
    {
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new GoogleAppsScriptOptions(null!, "token"));

        Assert.Equal("endpointUrl", exception.ParamName);
    }

    [Fact]
    public void GoogleAppsScriptOptions_NullTokenThrowsArgumentNullException()
    {
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new GoogleAppsScriptOptions("https://script.google.com/macros/s/deployment/exec", null!));

        Assert.Equal("sharedAccessToken", exception.ParamName);
    }

    [Fact]
    public void GoogleAppsScriptClientFactory_NullHttpClientThrowsArgumentNullException()
    {
        var options = new GoogleAppsScriptOptions(
            "https://script.google.com/macros/s/deployment/exec",
            "secret-token");

        var exception = Assert.Throws<ArgumentNullException>(() =>
            GoogleAppsScriptClientFactory.Create(options, null!));

        Assert.Equal("httpClient", exception.ParamName);
    }

    [Fact]
    public async Task InvokeAsync_SendsFunctionAndPayloadEnvelope()
    {
        string? postedJson = null;
        using var httpClient = new HttpClient(new StubHttpMessageHandler(request =>
        {
            postedJson = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
            return JsonResponse("""{"success":true,"data":{"playerId":"player-1","level":12},"error":null}""");
        }));
        var client = CreateClient(httpClient);

        var result = await client.InvokeAsync<LoadPlayerRequest, LoadPlayerResponse>(
            "loadPlayer",
            new LoadPlayerRequest("player-1"));

        Assert.True(result.IsSuccess);
        Assert.Equal("player-1", result.Data?.PlayerId);
        Assert.Equal(12, result.Data?.Level);

        var payload = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(postedJson!)!;
        Assert.Equal("loadPlayer", payload["function"].GetString());
        Assert.Equal("player-1", payload["payload"].GetProperty("playerId").GetString());
    }

    [Fact]
    public async Task InvokeAsync_CallerPayloadCannotOverrideSharedTokenOrSource()
    {
        string? postedJson = null;
        using var httpClient = new HttpClient(new StubHttpMessageHandler(request =>
        {
            postedJson = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
            return JsonResponse("""{"success":true,"data":{"accepted":true},"error":null}""");
        }));
        var client = CreateClient(httpClient);

        var result = await client.InvokeAsync<MaliciousRequest, AcceptedResponse>(
            "save",
            new MaliciousRequest("caller-token", "CallerSource"));

        Assert.True(result.IsSuccess);
        var envelope = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(postedJson!)!;
        Assert.Equal("secret-token", envelope["token"].GetString());
        Assert.Equal("AndroidApp", envelope["source"].GetString());
        Assert.Equal("caller-token", envelope["payload"].GetProperty("token").GetString());
        Assert.Equal("CallerSource", envelope["payload"].GetProperty("source").GetString());
    }

    [Fact]
    public async Task InvokeAsync_SupportsNoPayloadFunctions()
    {
        string? postedJson = null;
        using var httpClient = new HttpClient(new StubHttpMessageHandler(request =>
        {
            postedJson = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
            return JsonResponse("""{"success":true,"data":{"status":"ok"},"error":null}""");
        }));
        var client = CreateClient(httpClient);

        var result = await client.InvokeAsync<HealthResponse>("health");

        Assert.True(result.IsSuccess);
        Assert.Equal("ok", result.Data?.Status);

        var envelope = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(postedJson!)!;
        Assert.Equal(JsonValueKind.Null, envelope["payload"].ValueKind);
    }

    [Fact]
    public async Task InvokeAsync_ResponseDoesNotDependOnBusinessFields()
    {
        using var httpClient = new HttpClient(new StubHttpMessageHandler(_ =>
            JsonResponse("""{"success":true,"data":{"status":"ok"},"error":null}""")));
        var client = CreateClient(httpClient);

        var result = await client.InvokeAsync<HealthResponse>("health");

        Assert.True(result.IsSuccess);
        Assert.Equal("ok", result.Data?.Status);
    }

    [Fact]
    public async Task InvokeAsync_MapsHttpFailureToTransportError()
    {
        using var httpClient = new HttpClient(new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.InternalServerError)));
        var client = CreateClient(httpClient);

        var result = await client.InvokeAsync<HealthResponse>("health");

        Assert.False(result.IsSuccess);
        Assert.Equal("HTTP_500", result.Error?.Code);
        Assert.Equal(RemoteFunctionErrorKind.Transport, result.Error?.Kind);
    }

    [Fact]
    public async Task InvokeAsync_Http401MapsToAuthenticationError()
    {
        using var httpClient = new HttpClient(new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.Unauthorized)));
        var client = CreateClient(httpClient);

        var result = await client.InvokeAsync<HealthResponse>("health");

        Assert.False(result.IsSuccess);
        Assert.Equal("HTTP_401", result.Error?.Code);
        Assert.Equal(RemoteFunctionErrorKind.Authentication, result.Error?.Kind);
        Assert.False(result.Error?.Retryable);
    }

    [Fact]
    public async Task InvokeAsync_Http403MapsToAuthorizationError()
    {
        using var httpClient = new HttpClient(new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.Forbidden)));
        var client = CreateClient(httpClient);

        var result = await client.InvokeAsync<HealthResponse>("health");

        Assert.False(result.IsSuccess);
        Assert.Equal("HTTP_403", result.Error?.Code);
        Assert.Equal(RemoteFunctionErrorKind.Authorization, result.Error?.Kind);
        Assert.False(result.Error?.Retryable);
    }

    [Fact]
    public async Task InvokeAsync_Http429MapsToRetryableRateLimitError()
    {
        using var httpClient = new HttpClient(new StubHttpMessageHandler(_ =>
            new HttpResponseMessage((HttpStatusCode)429)));
        var client = CreateClient(httpClient);

        var result = await client.InvokeAsync<HealthResponse>("health");

        Assert.False(result.IsSuccess);
        Assert.Equal("HTTP_429", result.Error?.Code);
        Assert.Equal(RemoteFunctionErrorKind.RateLimit, result.Error?.Kind);
        Assert.True(result.Error?.Retryable);
    }

    [Fact]
    public async Task InvokeAsync_MapsInvalidJsonToProtocolError()
    {
        using var httpClient = new HttpClient(new StubHttpMessageHandler(_ =>
            JsonResponse("""{"success":true,"data":""")));
        var client = CreateClient(httpClient);

        var result = await client.InvokeAsync<HealthResponse>("health");

        Assert.False(result.IsSuccess);
        Assert.Equal(RemoteFunctionErrorKind.Protocol, result.Error?.Kind);
    }

    [Fact]
    public async Task InvokeAsync_MalformedErrorEnvelopeMapsToProtocolError()
    {
        using var httpClient = new HttpClient(new StubHttpMessageHandler(_ =>
            JsonResponse("""{"success":false,"data":null,"error":{"retryable":false}}""")));
        var client = CreateClient(httpClient);

        var result = await client.InvokeAsync<HealthResponse>("health");

        Assert.False(result.IsSuccess);
        Assert.Equal("INVALID_RESPONSE", result.Error?.Code);
        Assert.Equal(RemoteFunctionErrorKind.Protocol, result.Error?.Kind);
    }

    [Fact]
    public async Task InvokeAsync_UnserializableRequestMapsToSerializationError()
    {
        var called = false;
        using var httpClient = new HttpClient(new StubHttpMessageHandler(_ =>
        {
            called = true;
            return JsonResponse("""{"success":true,"data":{"status":"ok"},"error":null}""");
        }));
        var client = CreateClient(httpClient);

        var result = await client.InvokeAsync<UnsupportedRequest, HealthResponse>(
            "unsupported",
            new UnsupportedRequest(() => { }));

        Assert.False(result.IsSuccess);
        Assert.Equal("SERIALIZATION_ERROR", result.Error?.Code);
        Assert.Equal(RemoteFunctionErrorKind.Serialization, result.Error?.Kind);
        Assert.False(called);
    }

    [Fact]
    public async Task InvokeAsync_ExternalCancellationIsNotMappedToTimeout()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();
        using var httpClient = new HttpClient(new StubHttpMessageHandler(_ =>
            JsonResponse("""{"success":true,"data":{"status":"ok"},"error":null}""")));
        var client = CreateClient(httpClient);

        await Assert.ThrowsAsync<TaskCanceledException>(() =>
            client.InvokeAsync<HealthResponse>("health", cancellationTokenSource.Token));
    }

    [Fact]
    public async Task InvokeAsync_InternalTimeoutMapsToTimeoutError()
    {
        using var httpClient = new HttpClient(new StubHttpMessageHandler(_ =>
            throw new TaskCanceledException("Simulated timeout.")));
        var client = CreateClient(httpClient);

        var result = await client.InvokeAsync<HealthResponse>("health");

        Assert.False(result.IsSuccess);
        Assert.Equal(RemoteFunctionErrorKind.Timeout, result.Error?.Kind);
    }

    [Fact]
    public async Task InvokeAsync_FoundRedirectUsesGet()
    {
        var methods = new List<HttpMethod>();
        using var httpClient = new HttpClient(new StubHttpMessageHandler(request =>
        {
            methods.Add(request.Method);
            if (methods.Count == 1)
            {
                return Redirect(HttpStatusCode.Found, "https://script.googleusercontent.com/macros/echo");
            }

            return JsonResponse("""{"success":true,"data":{"status":"ok"},"error":null}""");
        }));
        var client = CreateClient(httpClient);

        var result = await client.InvokeAsync<HealthResponse>("health");

        Assert.True(result.IsSuccess);
        Assert.Equal([HttpMethod.Post, HttpMethod.Get], methods);
    }

    [Fact]
    public async Task InvokeAsync_TemporaryRedirectKeepsPostBody()
    {
        var bodies = new List<string?>();
        var methods = new List<HttpMethod>();
        using var httpClient = new HttpClient(new StubHttpMessageHandler(request =>
        {
            methods.Add(request.Method);
            bodies.Add(request.Content?.ReadAsStringAsync().GetAwaiter().GetResult());
            if (methods.Count == 1)
            {
                return Redirect(HttpStatusCode.TemporaryRedirect, "https://script.googleusercontent.com/macros/echo");
            }

            return JsonResponse("""{"success":true,"data":{"status":"ok"},"error":null}""");
        }));
        var client = CreateClient(httpClient);

        var result = await client.InvokeAsync<HealthResponse>("health");

        Assert.True(result.IsSuccess);
        Assert.Equal([HttpMethod.Post, HttpMethod.Post], methods);
        Assert.False(string.IsNullOrWhiteSpace(bodies[0]));
        Assert.Equal(bodies[0], bodies[1]);
    }

    [Fact]
    public async Task InvokeAsync_RedirectToUnknownHostReturnsProtocolError()
    {
        var calls = 0;
        using var httpClient = new HttpClient(new StubHttpMessageHandler(_ =>
        {
            calls++;
            return Redirect(HttpStatusCode.TemporaryRedirect, "https://example.com/collect");
        }));
        var client = CreateClient(httpClient);

        var result = await client.InvokeAsync<HealthResponse>("health");

        Assert.False(result.IsSuccess);
        Assert.Equal("UNTRUSTED_REDIRECT_HOST", result.Error?.Code);
        Assert.Equal(RemoteFunctionErrorKind.Protocol, result.Error?.Kind);
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task InvokeAsync_InvalidConfigurationDoesNotSendHttpRequest()
    {
        var called = false;
        using var httpClient = new HttpClient(new StubHttpMessageHandler(_ =>
        {
            called = true;
            return JsonResponse("""{"success":true,"data":{"status":"ok"},"error":null}""");
        }));
        var options = new GoogleAppsScriptOptions("http://example.test", " ");
        var client = GoogleAppsScriptClientFactory.Create(options, httpClient);

        var result = await client.InvokeAsync<HealthResponse>("health");

        Assert.False(result.IsSuccess);
        Assert.Equal(RemoteFunctionErrorKind.Configuration, result.Error?.Kind);
        Assert.False(called);
    }

    private static HttpResponseMessage Redirect(HttpStatusCode statusCode, string location)
    {
        return new HttpResponseMessage(statusCode)
        {
            Headers =
            {
                Location = new Uri(location)
            }
        };
    }

    private static HttpResponseMessage JsonResponse(string json)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json)
        };
    }

    private static RemoteFunctions.Core.Application.IRemoteFunctionClient CreateClient(HttpClient httpClient)
    {
        var options = new GoogleAppsScriptOptions(
            "https://script.google.com/macros/s/deployment/exec",
            "secret-token",
            "AndroidApp");

        return GoogleAppsScriptClientFactory.Create(options, httpClient);
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _send;

        public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> send)
        {
            _send = send;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled<HttpResponseMessage>(cancellationToken);
            }

            return Task.FromResult(_send(request));
        }
    }

    private sealed record LoadPlayerRequest(string PlayerId);

    private sealed record LoadPlayerResponse(string PlayerId, int Level);

    private sealed record MaliciousRequest(string Token, string Source);

    private sealed record AcceptedResponse(bool Accepted);

    private sealed record HealthResponse(string Status);

    private sealed record UnsupportedRequest(Action Callback);
}
