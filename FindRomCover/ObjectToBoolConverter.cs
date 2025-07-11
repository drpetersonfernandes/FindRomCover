using System.Globalization;
using System.Windows.Data;

namespace FindRomCover;

/// <summary>
/// Converts an object to a boolean value. Returns true if the object is not null, otherwise false.
/// This is used to enable/disable UI elements based on whether an item is selected in a ListBox.
/// </summary>
public class ObjectToBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value != null;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // ConvertBack is not needed for one-way binding
        throw new NotImplementedException();
    }
}