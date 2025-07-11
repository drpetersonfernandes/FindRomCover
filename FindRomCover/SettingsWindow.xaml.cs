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
        var newExtension = await this.ShowInputAsync("Add Extension", "Enter the new file extension (without the dot):");

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
            _supportedExtensions.Add(newExtension);
            // Sort the list after adding
            var sorted = new ObservableCollection<string>(_supportedExtensions.OrderBy(i => i));
            _supportedExtensions.Clear();
            foreach (var item in sorted)
            {
                _supportedExtensions.Add(item);
            }

            LstSupportedExtensions.SelectedItem = newExtension;
        }
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
