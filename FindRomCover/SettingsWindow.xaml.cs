using System.Collections.ObjectModel;
using System.Windows;
using MahApps.Metro.Controls.Dialogs;

namespace FindRomCover;

public partial class SettingsWindow
{
    private readonly Settings _settings; // This will now always be App.Settings
    private readonly ObservableCollection<string> _supportedExtensions;

    public SettingsWindow(Settings settings) // This 'settings' parameter will be App.Settings
    {
        InitializeComponent();
        App.ApplyThemeToWindow(this);

        _settings = settings ?? throw new ArgumentNullException(nameof(settings));

        _supportedExtensions = new ObservableCollection<string>(_settings.SupportedExtensions.OrderBy(e => e));
        LstSupportedExtensions.ItemsSource = _supportedExtensions;
    }

    private async void BtnAdd_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var newExtension =
                await this.ShowInputAsync("Add Extension", "Enter the new file extension (without the dot):");

            if (string.IsNullOrWhiteSpace(newExtension))
            {
                return;
            }

            // Clean up the input
            newExtension = newExtension.Trim().Replace(".", "");

            // Validate the extension
            if (string.IsNullOrEmpty(newExtension))
            {
                await this.ShowMessageAsync("Invalid Input", "Extension cannot be empty or just a dot.");
                return;
            }

            // Check for valid characters in file extension
            if (!IsValidExtension(newExtension))
            {
                await this.ShowMessageAsync("Invalid Input",
                    "Extension contains invalid characters. Only letters, numbers, and hyphens are allowed.");
                return;
            }

            // Normalize to lowercase for consistency
            newExtension = newExtension.ToLowerInvariant();

            if (_supportedExtensions.Contains(newExtension, StringComparer.OrdinalIgnoreCase))
            {
                await this.ShowMessageAsync("Duplicate", $"The extension '{newExtension}' already exists.");
            }
            else
            {
                var insertIndex = FindInsertIndex(newExtension);
                _supportedExtensions.Insert(insertIndex, newExtension);

                LstSupportedExtensions.SelectedItem = newExtension;
            }
        }
        catch (Exception ex)
        {
            _ = LogErrors.LogErrorAsync(ex, "Error in method BtnAdd_Click");
        }
    }

    private static bool IsValidExtension(string extension)
    {
        // Check if extension contains only valid characters (letters, numbers, hyphens)
        // and is not empty
        if (string.IsNullOrEmpty(extension))
            return false;

        return extension.All(c => char.IsLetterOrDigit(c) || c == '-');
    }

    private int FindInsertIndex(string newExtension)
    {
        var left = 0;
        var right = _supportedExtensions.Count;

        while (left < right)
        {
            var mid = left + (right - left) / 2;
            if (string.Compare(_supportedExtensions[mid], newExtension, StringComparison.OrdinalIgnoreCase) < 0)
            {
                left = mid + 1;
            }
            else
            {
                right = mid;
            }
        }

        return left;
    }

    private void BtnRemove_Click(object sender, RoutedEventArgs e)
    {
        if (LstSupportedExtensions.SelectedItem is string selectedExtension)
        {
            _supportedExtensions.Remove(selectedExtension);
        }
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Validate all extensions before saving
            var invalidExtensions = _supportedExtensions
                .Where(ext => !IsValidExtension(ext))
                .ToList();

            if (invalidExtensions.Count != 0)
            {
                var invalidList = string.Join(", ", invalidExtensions);
                var result = MessageBox.Show(
                    $"The following extensions contain invalid characters: {invalidList}\n\n" +
                    "Only letters, numbers, and hyphens are allowed in file extensions.\n\n" +
                    "Would you like to remove these invalid extensions and save the rest?",
                    "Invalid Extensions", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    // Remove invalid extensions
                    foreach (var invalid in invalidExtensions)
                    {
                        _supportedExtensions.Remove(invalid);
                    }
                }
                else
                {
                    // User chose not to continue, exit without saving
                    return;
                }
            }

            // Ensure extensions are lowercase for consistency
            var normalizedExtensions = _supportedExtensions
                .Select(ext => ext.ToLowerInvariant())
                .OrderBy(ext => ext, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            _settings.SupportedExtensions = normalizedExtensions;
            _settings.SaveSettings();

            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            _ = LogErrors.LogErrorAsync(ex, "Error in BtnSave_Click");
            MessageBox.Show("An error occurred while saving settings. Your changes were not saved.",
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}