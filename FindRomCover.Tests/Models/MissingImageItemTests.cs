using FindRomCover.Models;
using FluentAssertions;

namespace FindRomCover.Tests.Models;

public class MissingImageItemTests
{
    [Fact]
    public void ConstructorSetsProperties()
    {
        var item = new MissingImageItem("sf2", "Street Fighter II");

        item.RomName.Should().Be("sf2");
        item.SearchName.Should().Be("Street Fighter II");
    }

    [Fact]
    public void ConstructorWithSameValuesSetsBoth()
    {
        var item = new MissingImageItem("mario", "mario");

        item.RomName.Should().Be("mario");
        item.SearchName.Should().Be("mario");
    }

    [Fact]
    public void ConstructorWithEmptyStringsSetsEmptyStrings()
    {
        var item = new MissingImageItem("", "");

        item.RomName.Should().BeEmpty();
        item.SearchName.Should().BeEmpty();
    }
}
