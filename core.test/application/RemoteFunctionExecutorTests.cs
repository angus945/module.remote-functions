using RemoteFunctions.Core.Application;
using RemoteFunctions.Core.Domain;

namespace RemoteFunctions.Core.Tests.Application;

public sealed class RemoteFunctionExecutorTests
{
    [Fact]
    public void Constructor_NullGatewayThrowsArgumentNullException()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => new RemoteFunctionExecutor(null!));

        Assert.Equal("gateway", exception.ParamName);
    }

    [Fact]
    public async Task ExecuteAsync_DelegatesInvocationAndCancellationTokenToGateway()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var request = new TestRequest("player-1");
        var gateway = new RecordingGateway<TestRequest, TestResponse>(
            RemoteFunctionResult<TestResponse>.Success(new TestResponse("ok")));
        var executor = new RemoteFunctionExecutor(gateway);
        var invocation = new RemoteFunctionInvocation<TestRequest>(
            new RemoteFunctionName("loadPlayer"),
            request);

        var result = await executor.ExecuteAsync<TestRequest, TestResponse>(
            invocation,
            cancellationTokenSource.Token);

        Assert.True(result.IsSuccess);
        Assert.Equal("loadPlayer", gateway.LastInvocation?.FunctionName.Value);
        Assert.Same(request, gateway.LastInvocation?.Request);
        Assert.Equal(cancellationTokenSource.Token, gateway.LastCancellationToken);
    }

    private sealed class RecordingGateway<TRequest, TResponse> : IRemoteFunctionGateway
    {
        private readonly RemoteFunctionResult<TResponse> _result;

        public RecordingGateway(RemoteFunctionResult<TResponse> result)
        {
            _result = result;
        }

        public RemoteFunctionInvocation<TRequest>? LastInvocation { get; private set; }

        public CancellationToken LastCancellationToken { get; private set; }

        public Task<RemoteFunctionResult<TGatewayResponse>> InvokeAsync<TGatewayRequest, TGatewayResponse>(
            RemoteFunctionInvocation<TGatewayRequest> invocation,
            CancellationToken cancellationToken = default)
        {
            LastInvocation = Assert.IsType<RemoteFunctionInvocation<TRequest>>(invocation);
            LastCancellationToken = cancellationToken;
            return Task.FromResult(Assert.IsType<RemoteFunctionResult<TGatewayResponse>>(_result));
        }
    }

    private sealed record TestRequest(string PlayerId);

    private sealed record TestResponse(string Status);
}
