using System.Globalization;
using FluentAssertions;
using FindRomCover.Services;
using Xunit;

namespace FindRomCover.Tests.Services;

public class ObjectToBoolConverterExtendedTests
{
    private readonly ObjectToBoolConverter _converter = new();

    [Fact]
    public void ConvertWithEmptyStringShouldReturnTrue()
    {
        var result = _converter.Convert("", typeof(bool), null, CultureInfo.InvariantCulture);

        result.Should().Be(true);
    }

    [Fact]
    public void ConvertWithFalseBooleanShouldReturnTrue()
    {
        var result = _converter.Convert(false, typeof(bool), null, CultureInfo.InvariantCulture);

        result.Should().Be(true);
    }

    [Fact]
    public void ConvertWithNegativeNumberShouldReturnTrue()
    {
        var result = _converter.Convert(-1, typeof(bool), null, CultureInfo.InvariantCulture);

        result.Should().Be(true);
    }

    [Fact]
    public void ConvertWithEmptyListShouldReturnTrue()
    {
        var result = _converter.Convert(new List<string>(), typeof(bool), null, CultureInfo.InvariantCulture);

        result.Should().Be(true);
    }

    [Fact]
    public void ConvertWithWhitespaceStringShouldReturnTrue()
    {
        var result = _converter.Convert("   ", typeof(bool), null, CultureInfo.InvariantCulture);

        result.Should().Be(true);
    }

    [Fact]
    public void ConvertBackWithAnyValueShouldThrowNotImplementedException()
    {
        Action act1 = () => _converter.ConvertBack(false, typeof(object), null, CultureInfo.InvariantCulture);
        Action act2 = () => _converter.ConvertBack(null, typeof(object), null, CultureInfo.InvariantCulture);
        Action act3 = () => _converter.ConvertBack("test", typeof(string), null, CultureInfo.InvariantCulture);

        act1.Should().Throw<NotImplementedException>();
        act2.Should().Throw<NotImplementedException>();
        act3.Should().Throw<NotImplementedException>();
    }
}
