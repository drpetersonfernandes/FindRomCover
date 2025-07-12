using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Documents;
using ControlzEx.Theming;
using FindRomCover.models;
using Microsoft.Win32;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace FindRomCover;

public partial class MainWindow : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    private readonly Settings _settings = new();
    private string _imageFolderPath;
    private string _selectedRomFileName = string.Empty;

    public ObservableCollection<ImageData> SimilarImages { get; set; } = [];
    private const string DefaultSimilarityAlgorithm = "Jaro-Winkler Distance";

    private int _imageWidth;

    public int ImageWidth
    {
        get => _imageWidth;
        set
        {
            if (_imageWidth == value) return;

            _imageWidth = value;
            OnPropertyChanged(nameof(ImageWidth));
        }
    }

    private int _imageHeight;

    public int ImageHeight
    {
        get => _imageHeight;
        set
        {
            if (_imageHeight == value) return;

            _imageHeight = value;
            OnPropertyChanged(nameof(ImageHeight));
        }
    }

    private bool _isCheckingMissing;

    public bool IsCheckingMissing
    {
        get => _isCheckingMissing;
        set
        {
            if (_isCheckingMissing == value) return;

            _isCheckingMissing = value;
            OnPropertyChanged(nameof(IsCheckingMissing));
        }
    }

    private bool _isFindingSimilar;

    public bool IsFindingSimilar
    {
        get => _isFindingSimilar;
        set
        {
            if (_isFindingSimilar == value) return;

            _isFindingSimilar = value;
            OnPropertyChanged(nameof(IsFindingSimilar));
        }
    }

    private string SelectedSimilarityAlgorithm { get; set; } = DefaultSimilarityAlgorithm;
    public object ImageSource { get; } = new();
    public object ImagePath { get; } = new();
    public object ImageName { get; } = new();
    public object SimilarityThreshold { get; } = new();

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;

        // Check for command-line arguments
        var args = Environment.GetCommandLineArgs();
        if (args.Length == 3)
        {
            // Validate that the provided paths are valid directories
            var imageFolderPath = args[1];
            var romFolderPath = args[2];

            if (Directory.Exists(imageFolderPath) && Directory.Exists(romFolderPath))
            {
                // args[1] is expected to be ImageFolder, args[2] to be RomFolder
                _imageFolderPath = imageFolderPath;
                TxtImageFolder.Text = _imageFolderPath;
                TxtRomFolder.Text = romFolderPath;
            }
            else
            {
                // Show error message for invalid paths
                var invalidPaths = new List<string>();
                if (!Directory.Exists(imageFolderPath))
                    invalidPaths.Add($"Image folder: '{imageFolderPath}'");
                if (!Directory.Exists(romFolderPath))
                    invalidPaths.Add($"ROM folder: '{romFolderPath}'");

                MessageBox.Show(
                    $"The following command-line paths are invalid or do not exist:\n\n{string.Join("\n", invalidPaths)}\n\nThe application will start with empty folder paths.",
                    "Invalid Command-Line Arguments", MessageBoxButton.OK, MessageBoxImage.Warning);

                // Set to empty values
                _imageFolderPath = "";
                TxtImageFolder.Text = "";
                TxtRomFolder.Text = "";
            }
        }
        else
        {
            // Proceed with regular execution if arguments are not as expected
            // or no arguments are provided
            _imageFolderPath = "";
            TxtImageFolder.Text = ""; // Set to default or empty if no arguments
            TxtRomFolder.Text = ""; // Set to default or empty if no arguments
        }

        LoadSettings();
        UpdateThumbnailSizeMenuChecks();
        UpdateSimilarityAlgorithmChecks();
        UpdateSimilarityThresholdChecks();
    }

    private void LoadSettings()
    {
        ImageWidth = _settings.ImageWidth;
        ImageHeight = _settings.ImageHeight;

        // Load and apply the theme
        ThemeManager.Current.ChangeThemeBaseColor(this, _settings.BaseTheme);
        ThemeManager.Current.ChangeThemeColorScheme(this, _settings.AccentColor);

        // Mark the correct menu item as checked
        if (_settings.BaseTheme == "Light")
        {
            LightTheme.IsChecked = true;
        }
        else
        {
            DarkTheme.IsChecked = true;
        }

        // Use iteration approach similar to other menu updates instead of FindName
        UpdateAccentColorChecks();
    }

    private void UpdateAccentColorChecks()
    {
        // Find the "Theme" menu item
        MenuItem? themeMenuItem = null;
        foreach (var item in MainMenu.Items)
        {
            if (item is not MenuItem mi || mi.Header?.ToString() != "Theme") continue;

            themeMenuItem = mi;
            break;
        }

        if (themeMenuItem == null)
        {
            // This should not happen if XAML is correct, but good to handle
            _ = LogErrors.LogErrorAsync(new Exception("Theme menu item not found."), "Theme menu item not found in MainMenu.");
            return;
        }

        // Find the "Accent Colors" submenu under "Theme"
        MenuItem? accentColorsMenuItem = null;
        foreach (var subItem in themeMenuItem.Items)
        {
            if (subItem is not MenuItem mi || mi.Header?.ToString() != "Accent Colors") continue;

            accentColorsMenuItem = mi;
            break;
        }

        if (accentColorsMenuItem == null)
        {
            // This should not happen if XAML is correct
            _ = LogErrors.LogErrorAsync(new Exception("Accent Colors menu item not found."), "Accent Colors menu item not found under Theme menu.");
            return;
        }

        // Now iterate through the actual accent color menu items
        var accentMenuFound = false;
        foreach (var accentMenuItem in accentColorsMenuItem.Items)
        {
            if (accentMenuItem is not MenuItem mi || !mi.Name.EndsWith("Accent", StringComparison.Ordinal)) continue;

            var accentName = mi.Name.Replace("Accent", "");
            mi.IsChecked = accentName == _settings.AccentColor;
            if (mi.IsChecked)
            {
                accentMenuFound = true;
            }
        }

        // If no matching accent color was found, log a warning and potentially set a default
        if (accentMenuFound || string.IsNullOrEmpty(_settings.AccentColor)) return;
        // Log the issue for debugging
        var warningMessage =
            $"Accent color '{_settings.AccentColor}' not found in menu items. Settings may be corrupted.";
        _ = LogErrors.LogErrorAsync(new Exception(warningMessage), warningMessage);

        // Optionally reset to a default accent color
        // You could uncomment the lines below to set a default
        // _settings.AccentColor = "Blue"; // or whatever your default is
        // _settings.SaveSettings();
    }

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void ChangeBaseTheme_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem menuItem) return;

        var theme = menuItem.Name == "LightTheme" ? "Light" : "Dark";
        ThemeManager.Current.ChangeThemeBaseColor(this, theme);

        // Save base theme to settings.xml
        _settings.BaseTheme = theme;
        _settings.SaveSettings();

        // Update menu item check state
        LightTheme.IsChecked = theme == "Light";
        DarkTheme.IsChecked = theme == "Dark";
    }

    private void ChangeAccentColor_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem menuItem) return;

        // Extract the accent color name from the selected menu item's name
        var accent = menuItem.Name.Replace("Accent", "");

        // Change the accent color of the application
        ThemeManager.Current.ChangeThemeColorScheme(this, accent);

        // Save the selected accent color to the settings.xml file
        _settings.AccentColor = accent;
        _settings.SaveSettings();

        // Use safer approach to uncheck all accent color options
        UncheckAllAccentColors(menuItem);

        // Check the currently selected accent color
        menuItem.IsChecked = true;
    }

    private void UncheckAllAccentColors(MenuItem selectedMenuItem)
    {
        // Find the "Theme" menu item
        MenuItem? themeMenuItem = null;
        foreach (var item in MainMenu.Items)
        {
            if (item is not MenuItem mi || mi.Header?.ToString() != "Theme") continue;

            themeMenuItem = mi;
            break;
        }

        if (themeMenuItem == null) return; // Should not happen, but defensively

        // Find the "Accent Colors" submenu under "Theme"
        MenuItem? accentColorsMenuItem = null;
        foreach (var subItem in themeMenuItem.Items)
        {
            if (subItem is not MenuItem mi || mi.Header?.ToString() != "Accent Colors") continue;

            accentColorsMenuItem = mi;
            break;
        }

        if (accentColorsMenuItem == null) return; // Should not happen, but defensively

        // Iterate through all accent color options and uncheck them
        foreach (var item in accentColorsMenuItem.Items)
        {
            if (item is MenuItem accentMenuItem && accentMenuItem.Name.EndsWith("Accent", StringComparison.Ordinal))
            {
                accentMenuItem.IsChecked = false;
            }
        }
        // The calling method ChangeAccentColor_Click will then set selectedMenuItem.IsChecked = true;
    }

    // This method is no longer needed as its logic is integrated into UncheckAllAccentColors
    // private void UncheckAccentItemsInMenu(MenuItem parentMenu, MenuItem selectedMenuItem)
    // {
    //     foreach (var item in parentMenu.Items)
    //     {
    //         if (item is not MenuItem menuItem) continue;
    //
    //         if (menuItem.Name.EndsWith("Accent", StringComparison.Ordinal))
    //         {
    //             menuItem.IsChecked = false;
    //         }
    //
    //         // Recursively check submenus if they exist
    //         if (menuItem.Items.Count > 0)
    //         {
    //             UncheckAccentItemsInMenu(menuItem, selectedMenuItem);
    //         }
    //     }
    // }

    private void DonateButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "https://www.purelogiccode.com/donate",
                UseShellExecute = true
            };
            System.Diagnostics.Process.Start(psi);
        }
        catch (Exception ex)
        {
            // Notify user
            MessageBox.Show($"Unable to open the donation link: {ex.Message}",
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);

            // Notify developer
            var formattedException = $"Unable to open the donation link.\n\n" +
                                     $"Exception type: {ex.GetType().Name}\n" +
                                     $"Exception details: {ex.Message}";
            _ = LogErrors.LogErrorAsync(ex, formattedException);
        }
    }

    private void About_Click(object sender, RoutedEventArgs e)
    {
        About aboutWindow = new();
        aboutWindow.ShowDialog();
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void BtnBrowseRomFolder_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Select the folder where your ROM or ISO files are stored."
        };

        if (dialog.ShowDialog() == true)
        {
            TxtRomFolder.Text = dialog.FolderName;
        }
    }

    private void BtnBrowseImageFolder_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Select the folder where your image files are stored."
        };

        if (dialog.ShowDialog() != true) return;

        TxtImageFolder.Text = dialog.FolderName;
        _imageFolderPath = dialog.FolderName;
    }

    private async void BtnCheckForMissingImages_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await LoadMissingImagesList();
        }
        catch (Exception ex)
        {
            // Notify developer
            _ = LogErrors.LogErrorAsync(ex, "Error in BtnCheckForMissingImages_Click");
        }
    }

    private async Task LoadMissingImagesList()
    {
        if (_settings.SupportedExtensions.Length == 0)
        {
            // Notify user
            MessageBox.Show("No supported file extensions loaded. Please check file 'settings.xml' or edit them in the Settings menu.",
                "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);

            return;
        }

        // Capture UI-dependent values before going to a background thread
        var romFolderPath = TxtRomFolder.Text;
        var imageFolderPath = TxtImageFolder.Text;

        if (string.IsNullOrEmpty(romFolderPath) || string.IsNullOrEmpty(imageFolderPath))
        {
            // Notify user
            MessageBox.Show("Please select both ROM and Image folders.",
                "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);

            return;
        }

        IsCheckingMissing = true;
        try
        {
            // Get all ROM files and find missing ones on a background thread
            var missingFiles = await Task.Run(() =>
            {
                var searchPatterns = _settings.SupportedExtensions.Select(ext => "*." + ext).ToArray();
                var allRomNames = searchPatterns
                    .SelectMany(pattern => Directory.GetFiles(romFolderPath, pattern))
                    .Select(Path.GetFileNameWithoutExtension)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                var missing = new List<string>();
                foreach (var romName in allRomNames)
                {
                    if (romName != null && FindCorrespondingImage(romName, imageFolderPath) == null)
                    {
                        missing.Add(romName);
                    }
                }

                return missing.OrderBy(static name => name).ToList();
            });

            // Now, update the UI on the UI thread in one go
            LstMissingImages.Items.Clear();
            SimilarImages.Clear(); // Also clear suggestions
            foreach (var file in missingFiles)
            {
                LstMissingImages.Items.Add(file);
            }

            // Update count
            UpdateMissingCount();
        }
        catch (Exception ex)
        {
            // Notify user
            MessageBox.Show($"There was an error checking for the missing image files.\n\n" +
                            $"Check if the provided folders are valid.",
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);

            // Notify developer
            var formattedException = $"There was an error checking for the missing image files.\n\n" +
                                     $"Exception type: {ex.GetType().Name}\n" +
                                     $"Exception details: {ex.Message}";
            _ = LogErrors.LogErrorAsync(ex, formattedException);
        }
        finally
        {
            IsCheckingMissing = false;
        }
    }

    private static string? FindCorrespondingImage(string fileNameWithoutExtension, string imageFolderPath)
    {
        string[] imageExtensions = [".png", ".jpg", ".jpeg"];
        foreach (var ext in imageExtensions)
        {
            var imagePath = Path.Combine(imageFolderPath, fileNameWithoutExtension + ext);
            if (File.Exists(imagePath))
            {
                return imagePath;
            }
        }

        return null;
    }

    private async void LstMissingImages_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        try
        {
            if (LstMissingImages.SelectedItem is not string selectedFile)
            {
                SimilarImages.Clear();
                return;
            }

            IsFindingSimilar = true;
            try
            {
                _selectedRomFileName = selectedFile;

                // Use ButtonFactory to create the SimilarImages collection
                var newSimilarImages = await ButtonFactory.CreateSimilarImagesCollection(
                    selectedFile,
                    _imageFolderPath,
                    _settings.SimilarityThreshold,
                    SelectedSimilarityAlgorithm
                );

                // Update the UI
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    // Display the search query
                    var textBlock = new TextBlock();
                    textBlock.Inlines.Add(new Run("Search Query: "));
                    textBlock.Inlines.Add(new Run($"{selectedFile} ") { FontWeight = FontWeights.Bold });
                    textBlock.Inlines.Add(new Run("with "));
                    textBlock.Inlines.Add(new Run($"{SelectedSimilarityAlgorithm} ") { FontWeight = FontWeights.Bold });
                    textBlock.Inlines.Add(new Run("algorithm"));
                    LblSearchQuery.Content = textBlock;

                    // Clear and update the SimilarImages collection
                    SimilarImages.Clear();
                    foreach (var imageData in newSimilarImages)
                    {
                        SimilarImages.Add(imageData);
                    }
                });
            }
            catch (Exception ex)
            {
                // Notify user
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                // Notify developer
                _ = LogErrors.LogErrorAsync(ex, "Error in LstMissingImages_SelectionChanged");
            }
            finally
            {
                IsFindingSimilar = false;
            }
        }
        catch (Exception ex)
        {
            // Notify developer
            _ = LogErrors.LogErrorAsync(ex, "Error in LstMissingImages_SelectionChanged");
        }
    }

    private void ImageCell_MouseDown(object sender, MouseButtonEventArgs e)
    {
        // Ensure the click is a left mouse button click
        if (e.ChangedButton == MouseButton.Left && sender is FrameworkElement
            {
                DataContext: ImageData
                {
                    ImagePath: not null
                } imageData
            })
            UseImage(imageData.ImagePath);
    }

    public void UseImage(string imagePath)
    {
        if (string.IsNullOrEmpty(_selectedRomFileName) ||
            string.IsNullOrEmpty(imagePath) ||
            string.IsNullOrEmpty(_imageFolderPath)) return;

        var newFileName = Path.Combine(_imageFolderPath, _selectedRomFileName + ".png");
        if (ImageProcessor.ConvertAndSaveImage(imagePath, newFileName))
        {
            PlaySound.PlayClickSound();
            RemoveSelectedItem();
            SimilarImages.Clear();
            UpdateMissingCount(); // Update count whenever an item is removed
        }
        else
        {
            // Notify user
            MessageBox.Show("Failed to save the image.",
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);

            // Notify developer
            const string formattedException = "Failed to save the image.";
            var ex = new Exception(formattedException);
            _ = LogErrors.LogErrorAsync(ex, formattedException);
        }
    }

    private void BtnRemoveSelectedItem_Click(object sender, RoutedEventArgs e)
    {
        RemoveSelectedItem();
        SimilarImages.Clear();
        PlaySound.PlayClickSound();
    }

    private void RemoveSelectedItem()
    {
        if (LstMissingImages.SelectedItem == null) return;

        LstMissingImages.Items.Remove(LstMissingImages.SelectedItem);
        UpdateMissingCount();
    }

    private void UpdateMissingCount()
    {
        LabelMissingRoms.Content = "MISSING COVERS: " + LstMissingImages.Items.Count;
    }

    private void SetSimilarityAlgorithm_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem menuItem) return;

        SelectedSimilarityAlgorithm = menuItem.Header.ToString() ?? DefaultSimilarityAlgorithm;
        SaveSimilarityAlgorithmSetting(SelectedSimilarityAlgorithm);
        UncheckAllSimilarityAlgorithms(); // Uncheck all first
        menuItem.IsChecked = true; // Then check the selected one
    }

    private void UpdateSimilarityAlgorithmChecks()
    {
        foreach (var item in MenuSimilarityAlgorithms.Items)
        {
            if (item is MenuItem menuItem)
            {
                menuItem.IsChecked = menuItem.Header.ToString() == _settings.SelectedSimilarityAlgorithm;
            }
        }
    }

    private void UncheckAllSimilarityAlgorithms()
    {
        foreach (var item in MenuSimilarityAlgorithms.Items)
        {
            if (item is MenuItem menuItem)
            {
                menuItem.IsChecked = false; // Uncheck all
            }
        }
    }

    private void SaveSimilarityAlgorithmSetting(string algorithm)
    {
        _settings.SelectedSimilarityAlgorithm = algorithm;
        _settings.SaveSettings();
    }

    private void SetSimilarityThreshold_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem clickedItem) return;

        var headerText = clickedItem.Header.ToString()?.Replace("%", "") ?? "70";

        if (double.TryParse(headerText, out var rate))
        {
            _settings.SimilarityThreshold = rate;
            UncheckAllSimilarityThresholds(); // Uncheck all first
            clickedItem.IsChecked = true; // Then check the selected one
            _settings.SaveSettings();
        }
        else
        {
            // Notify user
            MessageBox.Show("Invalid similarity threshold selected.\n\n" +
                            "The error was reported to the developer that will try to fix the issue.",
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);

            // Notify developer
            const string formattedException = "Invalid similarity threshold selected.";
            var ex = new Exception(formattedException);
            _ = LogErrors.LogErrorAsync(ex, formattedException);
        }
    }

    private void UpdateSimilarityThresholdChecks()
    {
        foreach (var item in MySimilarityMenu.Items)
        {
            if (item is not MenuItem menuItem) continue;

            var thresholdString = menuItem.Header.ToString()?.Replace("%", "") ?? "70";
            if (double.TryParse(thresholdString, NumberStyles.Any, CultureInfo.InvariantCulture, out var menuItemThreshold))
            {
                // Check if this menu item's threshold matches the current setting
                menuItem.IsChecked = Math.Abs(menuItemThreshold - _settings.SimilarityThreshold) < 0.01;
            }
        }
    }

    private void UncheckAllSimilarityThresholds()
    {
        foreach (var item in MySimilarityMenu.Items)
        {
            if (item is not MenuItem menuItem) continue;

            menuItem.IsChecked = false; // Uncheck all
        }
    }

    private void SetThumbnailSize_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem { Header: not null } menuItem ||
            !int.TryParse(menuItem.Header.ToString()?.Split(' ')[0], out var size)) return;

        ImageWidth = size;
        ImageHeight = size;

        _settings.ImageWidth = size;
        _settings.ImageHeight = size;
        _settings.SaveSettings();

        foreach (var item in ImageSizeMenu.Items)
        {
            if (item is MenuItem sizeMenuItem)
            {
                sizeMenuItem.IsChecked = sizeMenuItem == menuItem;
            }
        }
    }

    private void UpdateThumbnailSizeMenuChecks()
    {
        var currentSize = ImageWidth;

        foreach (var item in ImageSizeMenu.Items)
        {
            if (item is not MenuItem menuItem) continue;
            // Assuming the header format is "{size} pixels", extract the number
            if (int.TryParse(menuItem.Header.ToString()?.Split(' ')[0], out var size))
            {
                menuItem.IsChecked = size == currentSize;
            }
        }
    }

    private void Image_ContextMenuOpening(object sender, ContextMenuEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: ImageData imageData } element) return;
        // Create and assign the context menu using ButtonFactory
        if (imageData.ImagePath != null)
        {
            element.ContextMenu = ButtonFactory.CreateContextMenu(imageData.ImagePath);
        }
    }

    private void EditExtensions_Click(object sender, RoutedEventArgs e)
    {
        var settingsWindow = new SettingsWindow(_settings)
        {
            Owner = this // Set the owner to center the dialog over the main window
        };
        settingsWindow.ShowDialog();
        // The _settings object is updated by reference, so no need to do anything else here.
        // The next time LoadMissingImagesList is called, it will use the new extensions.
    }

    private void TxtImageFolder_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            _imageFolderPath = textBox.Text;
        }
    }
}

