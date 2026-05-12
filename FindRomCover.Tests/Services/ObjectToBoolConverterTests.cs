using System.Globalization;
using FindRomCover.Services;
using FluentAssertions;

namespace FindRomCover.Tests.Services;

public class ObjectToBoolConverterTests
{
    private readonly ObjectToBoolConverter _converter = new();

    [Fact]
    public void ConvertNullReturnsFalse()
    {
        var result = _converter.Convert(null!, typeof(bool), null, CultureInfo.InvariantCulture);

        result.Should().Be(false);
    }

    [Fact]
    public void ConvertNonNullObjectReturnsTrue()
    {
        var result = _converter.Convert(new object(), typeof(bool), null, CultureInfo.InvariantCulture);

        result.Should().Be(true);
    }

    [Fact]
    public void ConvertNonNullStringReturnsTrue()
    {
        var result = _converter.Convert("test", typeof(bool), null, CultureInfo.InvariantCulture);

        result.Should().Be(true);
    }

    [Fact]
    public void ConvertNonNullIntegerReturnsTrue()
    {
        var result = _converter.Convert(42, typeof(bool), null, CultureInfo.InvariantCulture);

        result.Should().Be(true);
    }

    [Fact]
    public void ConvertBooleanFalseReturnsTrueBecauseItsNotNull()
    {
        var result = _converter.Convert(false, typeof(bool), null, CultureInfo.InvariantCulture);

        result.Should().Be(true);
    }

    [Fact]
    public void ConvertBackThrowsNotImplementedException()
    {
        var act = () => _converter.ConvertBack(true, typeof(object), null, CultureInfo.InvariantCulture);

        act.Should().Throw<NotImplementedException>();
    }

    [Fact]
    public void ConvertBackWithNullParameterThrowsNotImplementedException()
    {
        var act = () => _converter.ConvertBack(null, typeof(object), null, CultureInfo.InvariantCulture);

        act.Should().Throw<NotImplementedException>();
    }
}
