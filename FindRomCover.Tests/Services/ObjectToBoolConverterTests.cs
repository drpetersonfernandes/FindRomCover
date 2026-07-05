using System.Globalization;
using FluentAssertions;
using FindRomCover.Services;
using Xunit;

namespace FindRomCover.Tests.Services;

public class ObjectToBoolConverterTests
{
    private readonly ObjectToBoolConverter _converter = new();

    [Theory]
    [InlineData("some string", true)]
    [InlineData(123, true)]
    [InlineData(0, true)]
    public void ConvertWithNonNullValueShouldReturnTrue(object? value, bool expected)
    {
        var result = _converter.Convert(value, typeof(bool), null, CultureInfo.InvariantCulture);
        result.Should().Be(expected);
    }

    [Fact]
    public void ConvertWithNullValueShouldReturnFalse()
    {
        var result = _converter.Convert(null, typeof(bool), null, CultureInfo.InvariantCulture);
        result.Should().Be(false);
    }

    [Fact]
    public void ConvertBackShouldThrowNotImplementedException()
    {
        Action act = () => _converter.ConvertBack(true, typeof(object), null, CultureInfo.InvariantCulture);
        act.Should().Throw<NotImplementedException>();
    }
}
