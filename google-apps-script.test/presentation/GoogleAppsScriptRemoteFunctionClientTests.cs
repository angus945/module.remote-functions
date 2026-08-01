using System.Net;
using System.Text.Json;
using RemoteFunctions.GoogleAppsScript.Application;
using RemoteFunctions.GoogleAppsScript.Domain;
using RemoteFunctions.GoogleAppsScript.Presentation;

namespace RemoteFunctions.GoogleAppsScript.Tests.Presentation;

public sealed class GoogleAppsScriptRemoteFunctionClientTests
{
    [Fact]
    public async Task InvokeAsync_SendsPayloadWithTokenAndSource()
    {
        string? postedJson = null;
        using var httpClient = new HttpClient(new StubHttpMessageHandler(request =>
        {
            postedJson = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
            return JsonResponse("""{"ok":true,"recordId":"abc","duplicate":false}""");
        }));
        var client = CreateClient(httpClient);

        var result = await client.InvokeAsync(new Dictionary<string, object?>
        {
            ["recordId"] = "abc",
            ["amount"] = 42m,
            ["source"] = "CallerShouldNotWin"
        });

        Assert.True(result.IsSuccess);
        Assert.Equal("abc", result.Data["recordId"]);
        Assert.False((bool)result.Data["duplicate"]!);

        var payload = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(postedJson!)!;
        Assert.Equal("abc", payload["recordId"].GetString());
        Assert.Equal(42m, payload["amount"].GetDecimal());
        Assert.Equal("secret-token", payload["token"].GetString());
        Assert.Equal("AndroidApp", payload["source"].GetString());
    }

    [Fact]
    public async Task InvokeAsync_FollowsAppsScriptRedirect()
    {
        var calls = 0;
        using var httpClient = new HttpClient(new StubHttpMessageHandler(request =>
        {
            calls++;
            if (calls == 1)
            {
                return new HttpResponseMessage(HttpStatusCode.Found)
                {
                    Headers =
                    {
                        Location = new Uri("https://script.googleusercontent.com/macros/echo")
                    }
                };
            }

            Assert.Equal(HttpMethod.Get, request.Method);
            return JsonResponse("""{"ok":true,"recordId":"abc","duplicate":true}""");
        }));
        var client = CreateClient(httpClient);

        var result = await client.InvokeAsync(new Dictionary<string, object?>
        {
            ["recordId"] = "abc"
        });

        Assert.True(result.IsSuccess);
        Assert.Equal(2, calls);
        Assert.True((bool)result.Data["duplicate"]!);
    }

    [Fact]
    public async Task InvokeAsync_ReturnsNotConfiguredWithoutHttpRequest()
    {
        var called = false;
        using var httpClient = new HttpClient(new StubHttpMessageHandler(_ =>
        {
            called = true;
            return JsonResponse("""{"ok":true}""");
        }));
        var options = new GoogleAppsScriptRemoteFunctionOptions(
            "https://script.google.com/macros/s/REPLACE_WITH_DEPLOYMENT_ID/exec",
            "REPLACE_WITH_PRIVATE_API_TOKEN",
            "AndroidApp");
        var client = new GoogleAppsScriptRemoteFunctionClient(options, httpClient);

        var result = await client.InvokeAsync(new Dictionary<string, object?>
        {
            ["recordId"] = "abc"
        });

        Assert.False(result.IsSuccess);
        Assert.Equal(GoogleAppsScriptErrorCodes.NotConfigured, result.Error);
        Assert.False(called);
    }

    [Fact]
    public async Task InvokeAsync_AllowsPlaceholderTokenToReachBackend()
    {
        string? postedJson = null;
        using var httpClient = new HttpClient(new StubHttpMessageHandler(request =>
        {
            postedJson = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
            return JsonResponse("""{"ok":false,"error":"UNAUTHORIZED"}""");
        }));
        var options = new GoogleAppsScriptRemoteFunctionOptions(
            "https://script.google.com/macros/s/deployment/exec",
            "REPLACE_WITH_PRIVATE_API_TOKEN",
            "AndroidApp");
        var client = new GoogleAppsScriptRemoteFunctionClient(options, httpClient);

        var result = await client.InvokeAsync(new Dictionary<string, object?>
        {
            ["recordId"] = "abc"
        });

        Assert.False(result.IsSuccess);
        Assert.Equal("UNAUTHORIZED", result.Error);
        Assert.NotNull(postedJson);
    }

    [Fact]
    public async Task InvokeAsync_MapsHttpFailureToErrorCode()
    {
        using var httpClient = new HttpClient(new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.InternalServerError)));
        var client = CreateClient(httpClient);

        var result = await client.InvokeAsync(new Dictionary<string, object?>
        {
            ["recordId"] = "abc"
        });

        Assert.False(result.IsSuccess);
        Assert.Equal("HTTP_500", result.Error);
    }

    private static GoogleAppsScriptRemoteFunctionClient CreateClient(HttpClient httpClient)
    {
        var options = new GoogleAppsScriptRemoteFunctionOptions(
            "https://script.google.com/macros/s/deployment/exec",
            "secret-token",
            "AndroidApp");

        return new GoogleAppsScriptRemoteFunctionClient(options, httpClient);
    }

    private static HttpResponseMessage JsonResponse(string json)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json)
        };
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
            return Task.FromResult(_send(request));
        }
    }
}
