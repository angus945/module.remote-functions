using RemoteFunctions.Core.Application;
using RemoteFunctions.Core.Domain;
using RemoteFunctions.Core.Presentation;

namespace RemoteFunctions.Core.Tests.Presentation;

public sealed class RemoteFunctionClientTests
{
    [Fact]
    public void Constructor_NullExecutorThrowsArgumentNullException()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => new RemoteFunctionClient(null!));

        Assert.Equal("executor", exception.ParamName);
    }

    [Fact]
    public async Task InvokeAsync_SupportsTypedRequest()
    {
        var gateway = new RecordingGateway();
        var client = new RemoteFunctionClient(new RemoteFunctionExecutor(gateway));
        var request = new TestRequest("player-1");

        var result = await client.InvokeAsync<TestRequest, TestResponse>("loadPlayer", request);

        Assert.True(result.IsSuccess);
        Assert.Equal("loadPlayer", gateway.FunctionName);
        Assert.Same(request, gateway.Request);
    }

    [Fact]
    public async Task InvokeAsync_SupportsNoRequestFunctions()
    {
        var gateway = new RecordingGateway();
        var client = new RemoteFunctionClient(new RemoteFunctionExecutor(gateway));

        var result = await client.InvokeAsync<TestResponse>("health");

        Assert.True(result.IsSuccess);
        Assert.Equal("health", gateway.FunctionName);
        Assert.IsType<EmptyRemoteFunctionRequest>(gateway.Request);
    }

    [Fact]
    public void Client_IsApplicationInputPort()
    {
        Assert.IsAssignableFrom<IRemoteFunctionClient>(
            new RemoteFunctionClient(new RemoteFunctionExecutor(new RecordingGateway())));
    }

    private sealed class RecordingGateway : IRemoteFunctionGateway
    {
        public string? FunctionName { get; private set; }

        public object? Request { get; private set; }

        public Task<RemoteFunctionResult<TResponse>> InvokeAsync<TRequest, TResponse>(
            RemoteFunctionInvocation<TRequest> invocation,
            CancellationToken cancellationToken = default)
        {
            FunctionName = invocation.FunctionName.Value;
            Request = invocation.Request;
            var response = (TResponse)(object)new TestResponse("ok");
            return Task.FromResult(RemoteFunctionResult<TResponse>.Success(response));
        }
    }

    private sealed record TestRequest(string PlayerId);

    private sealed record TestResponse(string Status);
}
