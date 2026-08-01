using RemoteFunctions.Core.Domain;

namespace RemoteFunctions.Core.Tests.Domain;

public sealed class RemoteFunctionNameTests
{
    [Fact]
    public void Constructor_RejectsNullName()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => new RemoteFunctionName(null!));

        Assert.Equal("value", exception.ParamName);
    }

    [Fact]
    public void Constructor_RejectsEmptyName()
    {
        var exception = Assert.Throws<ArgumentException>(() => new RemoteFunctionName(" "));

        Assert.Equal("value", exception.ParamName);
    }

    [Fact]
    public void Constructor_TrimsName()
    {
        var name = new RemoteFunctionName(" health ");

        Assert.Equal("health", name.Value);
    }
}
