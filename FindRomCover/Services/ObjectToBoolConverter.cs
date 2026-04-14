using System.Globalization;
using System.Windows.Data;

namespace FindRomCover.Services;

/// <summary>
/// Converts an object to a boolean value based on null-checking.
/// </summary>
/// <remarks>
/// This converter returns true if the object is not null, otherwise false.
/// It's commonly used to enable/disable UI elements based on whether an item is selected
/// in a ListBox or other selector control.
/// 
/// Example usage in XAML:
/// <code>
/// IsEnabled="{Binding ElementName=MyListBox, Path=SelectedItem, Converter={StaticResource IsNotNullConverter}}"
/// </code>
/// </remarks>
public class ObjectToBoolConverter : IValueConverter
{
    /// <summary>
    /// Converts an object to a boolean value.
    /// </summary>
    /// <param name="value">The object to convert.</param>
    /// <param name="targetType">The type of the binding target property (unused).</param>
    /// <param name="parameter">Optional converter parameter (unused).</param>
    /// <param name="culture">The culture to use in the converter (unused).</param>
    /// <returns>true if value is not null; otherwise, false.</returns>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value != null;
    }

    /// <summary>
    /// Converts a boolean value back to an object.
    /// </summary>
    /// <param name="value">The boolean value to convert back.</param>
    /// <param name="targetType">The type to convert to.</param>
    /// <param name="parameter">Optional converter parameter.</param>
    /// <param name="culture">The culture to use in the converter.</param>
    /// <returns>Throws NotImplementedException as two-way binding is not supported.</returns>
    /// <exception cref="NotImplementedException">Always thrown as ConvertBack is not supported.</exception>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // ConvertBack is not needed for one-way binding
        throw new NotImplementedException();
    }
}
