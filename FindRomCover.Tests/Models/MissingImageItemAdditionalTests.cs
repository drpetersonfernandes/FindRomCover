using FluentAssertions;
using FindRomCover.Models;
using Xunit;

namespace FindRomCover.Tests.Models;

public class MissingImageItemAdditionalTests
{
    [Fact]
    public void ConstructorShouldSetRomName()
    {
        var item = new MissingImageItem("Super Mario Bros (USA)", "Super Mario Bros");

        item.RomName.Should().Be("Super Mario Bros (USA)");
    }

    [Fact]
    public void ConstructorShouldSetSearchName()
    {
        var item = new MissingImageItem("Super Mario Bros (USA)", "Super Mario Bros");

        item.SearchName.Should().Be("Super Mario Bros");
    }

    [Fact]
    public void ToStringShouldReturnRomName()
    {
        var item = new MissingImageItem("Super Mario Bros (USA)", "Super Mario Bros");

        var result = item.ToString();

        result.Should().Be("Super Mario Bros (USA)");
    }

    [Fact]
    public void ConstructorWithEmptyStringsShouldWork()
    {
        var item = new MissingImageItem("", "");

        item.RomName.Should().Be("");
        item.SearchName.Should().Be("");
    }

    [Fact]
    public void ConstructorWithSpecialCharactersShouldWork()
    {
        var item = new MissingImageItem("Game Name [v1.0] (USA) [!]", "Game Name");

        item.RomName.Should().Be("Game Name [v1.0] (USA) [!]");
        item.SearchName.Should().Be("Game Name");
    }

    [Fact]
    public void ConstructorWithUnicodeCharactersShouldWork()
    {
        var item = new MissingImageItem("ポケモン (Japan)", "ポケモン");

        item.RomName.Should().Be("ポケモン (Japan)");
        item.SearchName.Should().Be("ポケモン");
    }

    [Fact]
    public void ConstructorWithWhitespaceShouldPreserve()
    {
        var item = new MissingImageItem("  game name  ", "game name");

        item.RomName.Should().Be("  game name  ");
        item.SearchName.Should().Be("game name");
    }

    [Fact]
    public void ConstructorWithVeryLongNameShouldWork()
    {
        var longName = new string('a', 500);
        var item = new MissingImageItem(longName, "short");

        item.RomName.Should().HaveLength(500);
        item.SearchName.Should().Be("short");
    }

    [Fact]
    public void ToStringShouldMatchRomName()
    {
        var item = new MissingImageItem("Test Game (USA)", "Test Game");

        item.ToString().Should().Be(item.RomName);
    }

    [Fact]
    public void ConstructorWithDifferentRomAndSearchNamesShouldPreserveBoth()
    {
        var item = new MissingImageItem("Street Fighter II - Champion Edition (920513 etc)", "Street Fighter II");

        item.RomName.Should().Contain("920513");
        item.SearchName.Should().Be("Street Fighter II");
    }

    [Fact]
    public void ConstructorWithDotsShouldWork()
    {
        var item = new MissingImageItem("Dr. Mario (USA)", "Dr. Mario");

        item.RomName.Should().Be("Dr. Mario (USA)");
    }

    [Fact]
    public void ConstructorWithAmpersandShouldWork()
    {
        var item = new MissingImageItem("Tom & Jerry (USA)", "Tom & Jerry");

        item.RomName.Should().Be("Tom & Jerry (USA)");
    }

    [Fact]
    public void RomNameShouldBeReadOnly()
    {
        var item = new MissingImageItem("rom", "search");

        // RomName has no setter, verify it's accessible but matches constructor arg
        item.RomName.Should().Be("rom");
    }

    [Fact]
    public void SearchNameShouldBeReadOnly()
    {
        var item = new MissingImageItem("rom", "search");

        // SearchName has no setter, verify it's accessible but matches constructor arg
        item.SearchName.Should().Be("search");
    }

    [Fact]
    public void ToStringShouldReturnRomNameNotSearchName()
    {
        var item = new MissingImageItem("full_rom_name_with_tags", "clean_name");

        item.ToString().Should().Be("full_rom_name_with_tags");
        item.ToString().Should().NotBe("clean_name");
    }
}
