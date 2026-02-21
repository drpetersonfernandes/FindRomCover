using System.Collections.ObjectModel;
using System.Windows;
using FindRomCover.Managers;
using FindRomCover.Services;
using MahApps.Metro.Controls.Dialogs;
using MessageBox = System.Windows.MessageBox;

namespace FindRomCover;

public partial class SettingsWindow
{
    private readonly SettingsManager _settingsManager; // This will now always be App.Settings
    private readonly ObservableCollection<string> _supportedExtensions;

    public SettingsWindow(SettingsManager settingsManager)
    {
        InitializeComponent();
        App.ApplyThemeToWindow(this);

        _settingsManager = settingsManager ?? throw new ArgumentNullException(nameof(settingsManager));

        _supportedExtensions = new ObservableCollection<string>(_settingsManager.SupportedExtensions.OrderBy(static e => e, StringComparer.OrdinalIgnoreCase));
        DataContext = new { SupportedExtensions = _supportedExtensions };
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

            // Check for valid characters and length in file extension
            if (!IsValidExtension(newExtension))
            {
                await this.ShowMessageAsync("Invalid Input",
                    "Extension is invalid. It must be 1-10 characters and contain only letters, numbers, or hyphens.");
                return;
            }

            // Check for duplicates BEFORE normalization to catch any case variations
            if (_supportedExtensions.Contains(newExtension, StringComparer.OrdinalIgnoreCase))
            {
                await this.ShowMessageAsync("Duplicate",
                    $"The extension '{newExtension}' already exists (case-insensitive match).");
                return;
            }

            // Normalize to lowercase for consistency
            newExtension = newExtension.ToLowerInvariant();

            _supportedExtensions.Add(newExtension);

            LstSupportedExtensions.SelectedItem = newExtension;
        }
        catch (Exception ex)
        {
            _ = ErrorLogger.LogAsync(ex, "Error in method BtnAdd_Click");
        }
    }

    private static bool IsValidExtension(string extension)
    {
        const int maxExtensionLength = 10;
        // Check if extension contains only valid characters (letters, numbers, hyphens)
        // and is not empty or too long
        if (string.IsNullOrEmpty(extension) || extension.Length > maxExtensionLength)
            return false;

        return extension.All(static c => char.IsLetterOrDigit(c) || c == '-');
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
                .Where(static ext => !IsValidExtension(ext))
                .ToList();

            if (invalidExtensions.Count != 0)
            {
                var invalidList = string.Join(", ", invalidExtensions);
                var result = MessageBox.Show(
                    $"The following extensions are invalid: {invalidList}\n\n" +
                    "Extensions must be 1-10 characters long and contain only letters, numbers, and hyphens.\n\n" +
                    "Would you like to remove these invalid extensions and save the rest?",
                    "Invalid Extensions", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    // Remove invalid extensions
                    foreach (var invalid in invalidExtensions)
                    {
                        _supportedExtensions.Remove(invalid);
                    }

                    // Re-sort the ObservableCollection to maintain UI consistency
                    var sortedExtensions = _supportedExtensions.OrderBy(static ext => ext, StringComparer.OrdinalIgnoreCase)
                        .ToList();
                    _supportedExtensions.Clear();
                    foreach (var ext in sortedExtensions)
                    {
                        _supportedExtensions.Add(ext);
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
                .Select(static ext => ext.ToLowerInvariant())
                .OrderBy(static ext => ext, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            _settingsManager.SupportedExtensions = normalizedExtensions;
            _settingsManager.SaveSettings();

            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            _ = ErrorLogger.LogAsync(ex, "Error in BtnSave_Click");
            MessageBox.Show("An error occurred while saving settings. Your changes were not saved.",
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            DialogResult = false;
        }
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}