using System.Collections.ObjectModel;
using System.Windows;
using MahApps.Metro.Controls.Dialogs;

namespace FindRomCover;

public partial class SettingsWindow
{
    private readonly Settings _settings;
    private readonly ObservableCollection<string> _supportedExtensions;

    public SettingsWindow(Settings settings)
    {
        InitializeComponent();
        App.ApplyThemeToWindow(this);

        _settings = settings ?? throw new ArgumentNullException(nameof(settings));

        // Use an ObservableCollection to allow the ListBox to update automatically
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
                return; // User cancelled or entered nothing
            }

            // Clean up the input
            newExtension = newExtension.Trim().Replace(".", "");

            if (string.IsNullOrEmpty(newExtension))
            {
                await this.ShowMessageAsync("Invalid Input", "Extension cannot be empty or just a dot.");
                return;
            }

            if (_supportedExtensions.Contains(newExtension, StringComparer.OrdinalIgnoreCase))
            {
                await this.ShowMessageAsync("Duplicate", $"The extension '{newExtension}' already exists.");
            }
            else
            {
                // Find the correct index to insert the new extension to maintain sorted order
                var insertIndex = FindInsertIndex(newExtension);
                _supportedExtensions.Insert(insertIndex, newExtension);

                LstSupportedExtensions.SelectedItem = newExtension;
            }
        }
        catch (Exception ex)
        {
            // Notify developer
            _ = LogErrors.LogErrorAsync(ex, "Error in method BtnAdd_Click");
        }
    }

    private int FindInsertIndex(string newExtension)
    {
        // Use binary search to find the correct insertion point
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
        // Convert the collection back to an array and save
        _settings.SupportedExtensions = [.. _supportedExtensions];
        _settings.SaveSettings();

        DialogResult = true; // Indicates success
        Close();
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}