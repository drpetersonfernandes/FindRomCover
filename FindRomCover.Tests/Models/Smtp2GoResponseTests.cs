using FindRomCover.Models;
using FluentAssertions;

namespace FindRomCover.Tests.Models;

public class Smtp2GoResponseTests
{
    [Fact]
    public void Smtp2GoResponsePropertiesWork()
    {
        var response = new Smtp2GoResponse
        {
            Data = new Smtp2GoData
            {
                Succeeded = 1,
                Failed = 0,
                Errors = null
            }
        };

        response.Data.Should().NotBeNull();
        response.Data.Succeeded.Should().Be(1);
        response.Data.Failed.Should().Be(0);
        response.Data.Errors.Should().BeNull();
    }

    [Fact]
    public void Smtp2GoDataWithErrorsContainsErrors()
    {
        var data = new Smtp2GoData
        {
            Succeeded = 0,
            Failed = 1,
            Errors = ["Invalid API key"]
        };

        data.Succeeded.Should().Be(0);
        data.Failed.Should().Be(1);
        data.Errors.Should().ContainSingle().Which.Should().Be("Invalid API key");
    }

    [Fact]
    public void Smtp2GoResponseDefaultDataIsNull()
    {
        var response = new Smtp2GoResponse();

        response.Data.Should().BeNull();
    }
}
