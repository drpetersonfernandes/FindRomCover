using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Documents;
using ControlzEx.Theming;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace FindRomCover;

public partial class MainWindow : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    private readonly Settings _settings = new();
    private string _imageFolderPath;
    private string _selectedRomFileName = string.Empty;

    // ReSharper disable once MemberCanBePrivate.Global
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

    private string SelectedSimilarityAlgorithm { get; set; } = DefaultSimilarityAlgorithm;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;

        // Check for command-line arguments
        var args = Environment.GetCommandLineArgs();
        if (args.Length == 3)
        {
            // args[1] is expected to be ImageFolder, args[2] to be RomFolder
            _imageFolderPath = args[1];
            TxtImageFolder.Text = _imageFolderPath;
            TxtRomFolder.Text = args[2];
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

        if (FindName(_settings.AccentColor + "Accent") is MenuItem accentMenuItem)
        {
            accentMenuItem.IsChecked = true;
        }
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

        // Uncheck all accent color options before checking the new one
        foreach (var item in ((MenuItem)menuItem.Parent).Items)
        {
            if (item is MenuItem accentMenuItem)
            {
                accentMenuItem.IsChecked = false; // Uncheck all items
            }
        }

        // Check the currently selected accent color
        menuItem.IsChecked = true;
    }

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
            MessageBox.Show($"Unable to open the donation link: {ex.Message}",
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);

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
        var dialog = new FolderBrowserDialog
        {
            Description = "Select the folder where your ROM or ISO files are stored."
        };

        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            TxtRomFolder.Text = dialog.SelectedPath;
        }
    }

    private void BtnBrowseImageFolder_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new FolderBrowserDialog
        {
            Description = "Select the folder where your image files are stored."
        };

        if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;

        TxtImageFolder.Text = dialog.SelectedPath;
        _imageFolderPath = dialog.SelectedPath;
    }

    private void BtnCheckForMissingImages_Click(object sender, RoutedEventArgs e)
    {
        LoadMissingImagesList();
    }

    private void LoadMissingImagesList()
    {
        if (_settings.SupportedExtensions.Length == 0)
        {
            MessageBox.Show("No supported file extensions loaded. Please check file 'settings.xml'",
                "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrEmpty(TxtRomFolder.Text) || string.IsNullOrEmpty(TxtImageFolder.Text))
        {
            MessageBox.Show("Please select both ROM and Image folders.",
                "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            // Clear list before setting new values
            LstMissingImages.Items.Clear();

            // Create a HashSet to track unique filenames (case-insensitive)

            // Get all ROM files
            var searchPatterns = _settings.SupportedExtensions.Select(ext => "*." + ext).ToArray();
            var allFiles = searchPatterns
                .SelectMany(pattern => Directory.GetFiles(TxtRomFolder.Text, pattern))
                .Select(Path.GetFileNameWithoutExtension)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(name => name)
                .ToList();

            // Check each ROM for a corresponding image
            foreach (var romName in from romName in allFiles.Distinct(StringComparer.OrdinalIgnoreCase) let correspondingImagePath = FindCorrespondingImage(romName) where correspondingImagePath == null select romName)
            {
                LstMissingImages.Items.Add(romName);
            }

            // Update count
            UpdateMissingCount();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"There was an error checking for the missing image files.\n\n" +
                            $"Check if the provided folders are valid.",
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);

            var formattedException = $"There was an error checking for the missing image files.\n\n" +
                                     $"Exception type: {ex.GetType().Name}\n" +
                                     $"Exception details: {ex.Message}";
            _ = LogErrors.LogErrorAsync(ex, formattedException);
        }
    }

    private string? FindCorrespondingImage(string fileNameWithoutExtension)
    {
        string[] imageExtensions = [".png", ".jpg", ".jpeg"];
        foreach (var ext in imageExtensions)
        {
            var imagePath = Path.Combine(TxtImageFolder.Text, fileNameWithoutExtension + ext);
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
            if (LstMissingImages.SelectedItem is not string selectedFile) return;

            _selectedRomFileName = selectedFile;

            // Use ButtonFactory to create the SimilarImages collection
            var buttonFactory = new ButtonFactory();
            var newSimilarImages = await buttonFactory.CreateSimilarImagesCollection(
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
            // Handle exception
            MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
            MessageBox.Show("Failed to save the image.",
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);

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
        LabelMissingRoms.Content = "Missing Covers: " + LstMissingImages.Items.Count;
    }

    private void SetSimilarityAlgorithm_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem menuItem) return;

        SelectedSimilarityAlgorithm = menuItem.Header.ToString() ?? DefaultSimilarityAlgorithm;
        SaveSimilarityAlgorithmSetting(SelectedSimilarityAlgorithm);
        UncheckAllSimilarityAlgorithms();
        menuItem.IsChecked = true;
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
                // Check if the menuItem's header matches the SelectedSimilarityAlgorithm
                menuItem.IsChecked = menuItem.Header.ToString() == SelectedSimilarityAlgorithm;
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
            UncheckAllSimilarityThresholds();
            clickedItem.IsChecked = true;
            _settings.SaveSettings();
        }
        else
        {
            MessageBox.Show("Invalid similarity threshold selected.\n\n" +
                            "The error was reported to the developer that will try to fix the issue.",
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);

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

            if (double.TryParse(menuItem.Header.ToString()?.Replace("%", ""), out var rate))
            {
                menuItem.IsChecked = Math.Abs(rate - _settings.SimilarityThreshold) < 0.01; // Checking for equality in double
            }
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
}