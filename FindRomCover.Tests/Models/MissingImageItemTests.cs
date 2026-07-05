using FluentAssertions;
using FindRomCover.Models;
using Xunit;

namespace FindRomCover.Tests.Models;

public class MissingImageItemTests
{
    [Fact]
    public void ConstructorShouldSetRomName()
    {
        var item = new MissingImageItem("mario", "super mario");

        item.RomName.Should().Be("mario");
    }

    [Fact]
    public void ConstructorShouldSetSearchName()
    {
        var item = new MissingImageItem("mario", "super mario");

        item.SearchName.Should().Be("super mario");
    }

    [Fact]
    public void ToStringShouldReturnRomName()
    {
        var item = new MissingImageItem("mario", "super mario");

        var result = item.ToString();

        result.Should().Be("mario");
    }

    [Fact]
    public void ConstructorWithEmptyStringsShouldWork()
    {
        var item = new MissingImageItem("", "");

        item.RomName.Should().BeEmpty();
        item.SearchName.Should().BeEmpty();
    }

    [Fact]
    public void ConstructorWithSpecialCharactersShouldPreserveValues()
    {
        var item = new MissingImageItem("game (USA) [!]", "game");

        item.RomName.Should().Be("game (USA) [!]");
        item.SearchName.Should().Be("game");
    }
}
