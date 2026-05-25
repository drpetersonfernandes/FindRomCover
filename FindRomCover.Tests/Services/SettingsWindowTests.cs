using System.Reflection;
using FluentAssertions;

namespace FindRomCover.Tests.Services;

public class SettingsWindowTests
{
    private static readonly MethodInfo IsValidExtensionMethod = typeof(SettingsWindow)
        .GetMethod("IsValidExtension", BindingFlags.Static | BindingFlags.NonPublic)!;

    private static bool IsValidExtension(string? extension)
    {
        return (bool)IsValidExtensionMethod.Invoke(null, [extension])!;
    }

    #region Valid Extensions

    [Theory]
    [InlineData("nes")]
    [InlineData("sfc")]
    [InlineData("iso")]
    [InlineData("7z")]
    [InlineData("zip")]
    [InlineData("chd")]
    [InlineData("a-b")]
    [InlineData("123")]
    [InlineData("a")]
    [InlineData("1234567890")]
    [InlineData("gba")]
    [InlineData("nds")]
    public void IsValidExtension_WithValidExtensions_ReturnsTrue(string extension)
    {
        var result = IsValidExtension(extension);

        result.Should().BeTrue();
    }

    #endregion

    #region Invalid Extensions

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("too_long_ext")]
    [InlineData("ext!")]
    [InlineData("exe.ext")]
    [InlineData("dot.ext")]
    [InlineData("ext with space")]
    [InlineData("ext*")]
    [InlineData("ext/")]
    [InlineData("12345678901")]
    [InlineData("ext with spaces")]
    public void IsValidExtension_WithInvalidExtensions_ReturnsFalse(string? extension)
    {
        var result = IsValidExtension(extension);

        result.Should().BeFalse();
    }

    #endregion

    #region Boundary Cases

    [Fact]
    public void IsValidExtension_WithExactlyOneCharacter_ReturnsTrue()
    {
        var result = IsValidExtension("a");

        result.Should().BeTrue();
    }

    [Fact]
    public void IsValidExtension_WithExactlyTenCharacters_ReturnsTrue()
    {
        var result = IsValidExtension("abcdefghij");

        result.Should().BeTrue();
    }

    [Fact]
    public void IsValidExtension_WithElevenCharacters_ReturnsFalse()
    {
        var result = IsValidExtension("abcdefghijk");

        result.Should().BeFalse();
    }

    [Fact]
    public void IsValidExtension_WithHyphenOnly_ReturnsTrue()
    {
        var result = IsValidExtension("-");

        result.Should().BeTrue();
    }

    [Fact]
    public void IsValidExtension_WithMixedCase_ReturnsTrue()
    {
        var result = IsValidExtension("Nes");

        result.Should().BeTrue();
    }

    [Fact]
    public void IsValidExtension_WithUnderscore_ReturnsFalse()
    {
        var result = IsValidExtension("ext_name");

        result.Should().BeFalse();
    }

    #endregion
}
