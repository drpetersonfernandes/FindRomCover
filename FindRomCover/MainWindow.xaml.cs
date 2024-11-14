﻿using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Documents;
using ControlzEx.Theming;

namespace FindRomCover;

public partial class MainWindow : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    private readonly Settings _settings = new();
    private string _imageFolderPath;
    private string _selectedRomFileName = string.Empty;
    public ObservableCollection<ImageData> SimilarImages { get; set; } = new ObservableCollection<ImageData>();
    private const string DefaultSimilarityAlgorithm = "Jaro-Winkler Distance";
        
    private int _imageWidth;
    public int ImageWidth
    {
        get => _imageWidth;
        set
        {
            if (_imageWidth != value)
            {
                _imageWidth = value;
                OnPropertyChanged(nameof(ImageWidth));
            }
        }
    }

    private int _imageHeight;
    public int ImageHeight
    {
        get => _imageHeight;
        set
        {
            if (_imageHeight != value)
            {
                _imageHeight = value;
                OnPropertyChanged(nameof(ImageHeight));
            }
        }
    }
        
    private string SelectedSimilarityAlgorithm { get; set; } = DefaultSimilarityAlgorithm;
        
    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;
           
        // Check for command-line arguments
        string[] args = Environment.GetCommandLineArgs();
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
            TxtRomFolder.Text = "";    // Set to default or empty if no arguments
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

    private void OnPropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        
    private void ChangeBaseTheme_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem menuItem)
        {
            string theme = menuItem.Name == "LightTheme" ? "Light" : "Dark";
            ThemeManager.Current.ChangeThemeBaseColor(this, theme);

            // Save base theme to settings.xml
            _settings.BaseTheme = theme;
            _settings.SaveSettings();

            // Update menu item check state
            LightTheme.IsChecked = theme == "Light";
            DarkTheme.IsChecked = theme == "Dark";
        }
    }

    private void ChangeAccentColor_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem menuItem)
        {
            // Extract the accent color name from the selected menu item's name
            string accent = menuItem.Name.Replace("Accent", "");

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
                    accentMenuItem.IsChecked = false;  // Uncheck all items
                }
            }

            // Check the currently selected accent color
            menuItem.IsChecked = true;
        }
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
            System.Windows.MessageBox.Show($"Unable to open the donation link: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            
            string formattedException = $"Unable to open the donation link.\n\nException type: {ex.GetType().Name}\nException details: {ex.Message}";
            Task logTask = LogErrors.LogErrorAsync(ex, formattedException);
            logTask.Wait(TimeSpan.FromSeconds(2));
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

        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            TxtImageFolder.Text = dialog.SelectedPath;
            _imageFolderPath = dialog.SelectedPath;
        }
    }

    private void BtnCheckForMissingImages_Click(object sender, RoutedEventArgs e)
    {
        LoadMissingImagesList();
    }
        
    private void LoadMissingImagesList()
    {
        if (_settings.SupportedExtensions.Length == 0)
        {
            System.Windows.MessageBox.Show("No supported file extensions loaded. Please check file 'settings.xml'", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrEmpty(TxtRomFolder.Text) || string.IsNullOrEmpty(TxtImageFolder.Text))
        {
            System.Windows.MessageBox.Show("Please select both ROM and Image folders.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Clear list before setting new values
        LstMissingImages.Items.Clear();
        
        var searchPatterns = _settings.SupportedExtensions.Select(ext => "*." + ext).ToArray();
        var files = searchPatterns.SelectMany(ext => Directory.GetFiles(TxtRomFolder.Text, ext)).ToArray();
            
        CheckForMissingImages(files);
    }

    private void CheckForMissingImages(string[] romFiles)
    {
        foreach (string file in romFiles)
        {
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(file);
            string? correspondingImagePath = FindCorrespondingImage(fileNameWithoutExtension);

            if (correspondingImagePath == null)
            {
                LstMissingImages.Items.Add(fileNameWithoutExtension);
            }
        }
        // Update count whenever the check is performed
        UpdateMissingCount(); 

    }

        private string? FindCorrespondingImage(string fileNameWithoutExtension)
        {
            string[] imageExtensions = [".png", ".jpg", ".jpeg"];
            foreach (var ext in imageExtensions)
            {
                string imagePath = Path.Combine(TxtImageFolder.Text, fileNameWithoutExtension + ext);
                if (File.Exists(imagePath))
                {
                    return imagePath;
                }
            }
            return null;
        }

    private async void LstMissingImages_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (LstMissingImages.SelectedItem is string selectedFile)
        {
            _selectedRomFileName = selectedFile;
            var imageFolderPath = _imageFolderPath;
            var similarityThreshold = _settings.SimilarityThreshold;

            // Call the method and await its result
            var similarImages = await SimilarityCalculator.CalculateSimilarityAsync(selectedFile, imageFolderPath, similarityThreshold, SelectedSimilarityAlgorithm);

            // Update the UI accordingly
            // Assuming SimilarImages is an ObservableCollection bound to a UI control
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                // Update the label to display the search query
                var textBlock = new TextBlock();
                textBlock.Inlines.Add(new Run("Search Query: "));
                textBlock.Inlines.Add(new Run($"{selectedFile} ") { FontWeight = FontWeights.Bold });
                textBlock.Inlines.Add(new Run($"with "));
                textBlock.Inlines.Add(new Run($"{SelectedSimilarityAlgorithm} ") { FontWeight = FontWeights.Bold });
                textBlock.Inlines.Add(new Run($"algorithm"));
                LblSearchQuery.Content = textBlock;

                SimilarImages.Clear();
                foreach (var imageData in similarImages)
                {
                    SimilarImages.Add(imageData);
                }
            });
        }
    }

    public class ImageData
    {
        public string? ImagePath { get; init; }
        public string? ImageName { get; set; }
        public double SimilarityThreshold { get; init; }
    }

    private void ImageCell_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: ImageData imageData })
        {
            if (!string.IsNullOrEmpty(_selectedRomFileName) &&
                !string.IsNullOrEmpty(imageData.ImagePath) &&
                !string.IsNullOrEmpty(_imageFolderPath))
            {
                string newFileName = Path.Combine(_imageFolderPath, _selectedRomFileName + ".png");
                if (ConvertAndSaveImage(imageData.ImagePath, newFileName))
                {
                    PlaySound.PlayClickSound();
                    RemoveSelectedItem();
                    SimilarImages.Clear();
                    UpdateMissingCount(); // Update count whenever an item is removed
                }
                else
                {
                    System.Windows.MessageBox.Show("Failed to save the image.\n\nMaybe the application could not download the image file.","Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    
                    string formattedException = "Failed to save the image.\n\nMaybe the application could not download the image file.";
                    Exception ex = new Exception(formattedException);
                    Task logTask = LogErrors.LogErrorAsync(ex, formattedException);
                    logTask.Wait(TimeSpan.FromSeconds(2));
                }
            }
        }
    }

    private static bool ConvertAndSaveImage(string sourcePath, string targetPath)
    {
        try
        {
            using (var image = System.Drawing.Image.FromFile(sourcePath))
            {
                using var bitmap = new Bitmap(image);
                bitmap.Save(targetPath, System.Drawing.Imaging.ImageFormat.Png);
            }

            // Check if the file was saved successfully
            return File.Exists(targetPath);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Error saving image file\n\nMaybe the application does not have write privileges.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            
            string formattedException = $"Error saving image file\n\nMaybe the application does not have write privileges.\n\nException type: {ex.GetType().Name}\nException details: {ex.Message}";
            Task logTask = LogErrors.LogErrorAsync(ex, formattedException);
            logTask.Wait(TimeSpan.FromSeconds(2));
            
            return false;
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
        if (LstMissingImages.SelectedItem != null)
        {
            LstMissingImages.Items.Remove(LstMissingImages.SelectedItem);
            UpdateMissingCount();
        }
    }

    private void UpdateMissingCount()
    {
        LabelMissingRoms.Content = "Missing Covers: " + LstMissingImages.Items.Count;
    }

    private void SetSimilarityAlgorithm_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem menuItem)
        {
            SelectedSimilarityAlgorithm = menuItem.Header.ToString() ?? DefaultSimilarityAlgorithm;
            SaveSimilarityAlgorithmSetting(SelectedSimilarityAlgorithm);
            UncheckAllSimilarityAlgorithms();
            menuItem.IsChecked = true;
        }
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
        if (sender is MenuItem clickedItem)
        {
            string headerText = clickedItem.Header.ToString()?.Replace("%", "") ?? "70";

            if (double.TryParse(headerText, out double rate))
            {
                _settings.SimilarityThreshold = rate;
                UncheckAllSimilarityThresholds();
                clickedItem.IsChecked = true;
                _settings.SaveSettings();
            }
            else
            {
                System.Windows.MessageBox.Show("Invalid similarity threshold selected.\n\nThe error was reported to the developer that will try to fix the issue.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                
                string formattedException = "Invalid similarity threshold selected.";
                Exception ex = new Exception(formattedException);
                Task logTask = LogErrors.LogErrorAsync(ex, formattedException);
                logTask.Wait(TimeSpan.FromSeconds(2));
            }
        }
    }

    private void UpdateSimilarityThresholdChecks()
    {
        foreach (var item in MySimilarityMenu.Items)
        {
            if (item is MenuItem menuItem)
            {
                string thresholdString = menuItem.Header.ToString()?.Replace("%", "") ?? "70";
                if (double.TryParse(thresholdString, NumberStyles.Any, CultureInfo.InvariantCulture, out double menuItemThreshold))
                {
                    // Check if this menu item's threshold matches the current setting
                    menuItem.IsChecked = Math.Abs(menuItemThreshold - _settings.SimilarityThreshold) < 0.01;
                }
            }
        }
    }

    private void UncheckAllSimilarityThresholds()
    {
        foreach (var item in MySimilarityMenu.Items)
        {
            if (item is MenuItem menuItem)
            {
                if (double.TryParse(menuItem.Header.ToString()?.Replace("%", ""), out double rate))
                {
                    menuItem.IsChecked = Math.Abs(rate - _settings.SimilarityThreshold) < 0.01; // Checking for equality in double
                }
            }
        }
    }

    private void SetThumbnailSize_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem { Header: not null } menuItem && int.TryParse(menuItem.Header.ToString()?.Split(' ')[0], out int size))
        {
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
    }

    private void UpdateThumbnailSizeMenuChecks()
    {
        int currentSize = ImageWidth;

        foreach (var item in ImageSizeMenu.Items)
        {
            if (item is MenuItem menuItem)
            {
                // Assuming the header format is "{size} pixels", extract the number
                if (int.TryParse(menuItem.Header.ToString()?.Split(' ')[0], out int size))
                {
                    menuItem.IsChecked = size == currentSize;
                }
            }
        }
    }

}