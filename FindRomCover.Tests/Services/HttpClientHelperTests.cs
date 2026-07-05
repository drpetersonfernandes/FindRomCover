using FluentAssertions;
using FindRomCover.Services;
using Xunit;

namespace FindRomCover.Tests.Services;

public class HttpClientHelperTests : IDisposable
{
    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void ClientShouldReturnHttpClientInstance()
    {
        // Note: This test assumes HttpClientHelper has not been disposed in the test run.
        // If Dispose() was called elsewhere, this will throw ObjectDisposedException.
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
    public void ClientShouldHaveDefaultTimeout()
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
