using RemoteFunctions.Core.Application;
using RemoteFunctions.Core.Domain;

namespace RemoteFunctions.Core.Tests.Application;

public sealed class RemoteFunctionResultTests
{
    [Fact]
    public void Success_HasDataAndNoError()
    {
        var data = new TestResponse("ok");

        var result = RemoteFunctionResult<TestResponse>.Success(data);

        Assert.True(result.IsSuccess);
        Assert.Same(data, result.Data);
        Assert.Null(result.Error);
    }

    [Fact]
    public void Failure_HasErrorAndNoData()
    {
        var error = new RemoteFunctionError(
            "FAILED",
            "The remote function failed.",
            RemoteFunctionErrorKind.RemoteExecution);

        var result = RemoteFunctionResult<TestResponse>.Failure(error);

        Assert.False(result.IsSuccess);
        Assert.Null(result.Data);
        Assert.Same(error, result.Error);
    }

    [Fact]
    public void Type_HasNoPublicConstructors()
    {
        Assert.Empty(typeof(RemoteFunctionResult<TestResponse>).GetConstructors());
    }

    private sealed record TestResponse(string Status);
}
