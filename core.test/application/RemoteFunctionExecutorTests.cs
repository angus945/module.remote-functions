using RemoteFunctions.Core.Application;
using RemoteFunctions.Core.Domain;

namespace RemoteFunctions.Core.Tests.Application;

public sealed class RemoteFunctionExecutorTests
{
    [Fact]
    public async Task ExecuteAsync_DelegatesToGateway()
    {
        var gateway = new RecordingGateway(RemoteFunctionResult.Success());
        var executor = new RemoteFunctionExecutor(gateway);
        var call = RemoteFunctionCall.From(new Dictionary<string, object?>
        {
            ["recordId"] = "record-1"
        });

        var result = await executor.ExecuteAsync(call);

        Assert.True(result.IsSuccess);
        Assert.Same(call, gateway.LastCall);
    }

    private sealed class RecordingGateway : IRemoteFunctionGateway
    {
        private readonly RemoteFunctionResult _result;

        public RecordingGateway(RemoteFunctionResult result)
        {
            _result = result;
        }

        public RemoteFunctionCall? LastCall { get; private set; }

        public Task<RemoteFunctionResult> InvokeAsync(
            RemoteFunctionCall call,
            CancellationToken cancellationToken = default)
        {
            LastCall = call;
            return Task.FromResult(_result);
        }
    }
}
