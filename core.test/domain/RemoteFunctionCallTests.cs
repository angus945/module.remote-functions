using RemoteFunctions.Core.Domain;

namespace RemoteFunctions.Core.Tests.Domain;

public sealed class RemoteFunctionCallTests
{
    [Fact]
    public void Constructor_CopiesArguments()
    {
        var arguments = new Dictionary<string, object?>
        {
            ["amount"] = 10m
        };

        var call = RemoteFunctionCall.From(arguments);
        arguments["amount"] = 20m;

        Assert.Equal(10m, call.Arguments["amount"]);
    }

    [Fact]
    public void Constructor_RejectsEmptyArguments()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            RemoteFunctionCall.From(new Dictionary<string, object?>()));

        Assert.Equal("arguments", exception.ParamName);
    }

    [Fact]
    public void Constructor_RejectsEmptyArgumentName()
    {
        var arguments = new Dictionary<string, object?>
        {
            [" "] = "value"
        };

        var exception = Assert.Throws<ArgumentException>(() => RemoteFunctionCall.From(arguments));

        Assert.Equal("arguments", exception.ParamName);
    }
}
