using FluentAssertions;
using FindRomCover.Services;
using Xunit;

namespace FindRomCover.Tests.Services;

public class HttpClientHelperAdditionalTests
{
    [Fact]
    public void ClientShouldNotBeNull()
    {
        var client = HttpClientHelper.Client;

        client.Should().NotBeNull();
    }

    [Fact]
    public void ClientShouldReturnSameInstanceOnMultipleCalls()
    {
        var client1 = HttpClientHelper.Client;
        var client2 = HttpClientHelper.Client;

        client1.Should().BeSameAs(client2);
    }

    [Fact]
    public void ClientDefaultTimeoutShouldBe30Seconds()
    {
        var client = HttpClientHelper.Client;

        client.Timeout.Should().Be(TimeSpan.FromSeconds(30));
    }

    [Fact]
    public void ClientShouldHaveConnectionCloseSetToFalse()
    {
        var client = HttpClientHelper.Client;

        client.DefaultRequestHeaders.ConnectionClose.Should().BeFalse();
    }
}
