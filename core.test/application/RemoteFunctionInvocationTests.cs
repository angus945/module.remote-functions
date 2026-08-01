using RemoteFunctions.Core.Application;
using RemoteFunctions.Core.Domain;

namespace RemoteFunctions.Core.Tests.Application;

public sealed class RemoteFunctionInvocationTests
{
    [Fact]
    public void Constructor_NullFunctionNameThrowsArgumentNullException()
    {
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new RemoteFunctionInvocation<TestRequest>(null!, new TestRequest("player-1")));

        Assert.Equal("functionName", exception.ParamName);
    }

    [Fact]
    public void Constructor_NullRequestThrowsArgumentNullException()
    {
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new RemoteFunctionInvocation<TestRequest>(new RemoteFunctionName("loadPlayer"), null!));

        Assert.Equal("request", exception.ParamName);
    }

    private sealed record TestRequest(string PlayerId);
}
