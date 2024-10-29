using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using ControlzEx.Theming;

namespace FindRomCover
{
    public partial class MainWindow : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        private readonly Settings _settings = new();
        private string? _imageFolderPath;
        private string? _selectedRomFileName;
        public ObservableCollection<ImageData> SimilarImages { get; set; } = [];
        
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
        
        private string SelectedSimilarityAlgorithm { get; set; } = "Jaro-Winkler Distance"; // Default value
        
        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            
            LoadSettings();
            
            // Check for command-line arguments
            string[] args = Environment.GetCommandLineArgs();
            if (args.Length > 2)
            {
                // args[1] is expected to be ImageFolder, args[2] to be RomFolder
                _imageFolderPath = args[1];
                TxtImageFolder.Text = _imageFolderPath;
        
                TxtRomFolder.Text = args[2];
            }
            
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

        protected virtual void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        
        private void ChangeBaseTheme_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem)
            {
                string theme = menuItem.Name == "LightTheme" ? "Light" : "Dark";
                ThemeManager.Current.ChangeThemeBaseColor(this, theme);

                // Save base theme to settings.xml
                Settings.SaveSetting("BaseTheme", theme);

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
                Settings.SaveSetting("AccentColor", accent);

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
                System.Windows.MessageBox.Show("Unable to open the link: " + ex.Message);
            }
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            var version1 = Assembly.GetExecutingAssembly().GetName().Version;
            if (version1 != null)
            {
                string version = version1.ToString();
                System.Windows.MessageBox.Show($"Find Rom Cover\n\nVersion {version}\n\nhttps://purelogiccode.com", "About");
            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void BtnBrowseRomFolder_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new FolderBrowserDialog
            {
                Description = "Select the folder where your ROM files are stored."
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
                System.Windows.MessageBox.Show("No supported file extensions loaded. Please check your settings.xml.");
                return;
            }

            if (string.IsNullOrEmpty(TxtRomFolder.Text) || string.IsNullOrEmpty(TxtImageFolder.Text))
            {
                System.Windows.MessageBox.Show("Please select both ROM and Image folders.");
                return;
            }

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
            UpdateMissingCount(); // Update count whenever the check is performed

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
                var imageFolderPath = this._imageFolderPath;
                var similarityThreshold = this._settings.SimilarityThreshold;

                // Call the method and await its result
                if (imageFolderPath != null)
                {
                    var similarImages = await SimilarityCalculator.CalculateSimilarityAsync(selectedFile, imageFolderPath, similarityThreshold, SelectedSimilarityAlgorithm);

                    // Update the UI accordingly
                    // Assuming SimilarImages is an ObservableCollection bound to a UI control
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        // Update the label to display the search query
                        LblSearchQuery.Content = "Similarity Algorithm: " + SelectedSimilarityAlgorithm + "\nSearch Query: " + selectedFile;

                        SimilarImages.Clear();
                        foreach (var imageData in similarImages)
                        {
                            SimilarImages.Add(imageData);
                        }
                    });
                }
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
                        System.Windows.MessageBox.Show("Failed to save the image. Please try again.");
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
                System.Windows.MessageBox.Show($"Error saving file: {ex.Message}");
                return false;
            }
        }

        private void BtnRemoveSelectedItem_Click(object sender, RoutedEventArgs e)
        {
            RemoveSelectedItem();
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
                SelectedSimilarityAlgorithm = menuItem.Header.ToString() ?? "Jaro-Winkler Distance";
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

        private static void SaveSimilarityAlgorithmSetting(string algorithm)
        {
            Settings.SaveSetting("SimilarityAlgorithm", algorithm);
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
                    Settings.SaveSetting("SimilarityThreshold", _settings.SimilarityThreshold.ToString(CultureInfo.InvariantCulture));
                }
                else
                {
                    System.Windows.MessageBox.Show("Invalid similarity rate selected.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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

                Settings.SaveSetting("ImageSize/Width", size.ToString());
                Settings.SaveSetting("ImageSize/Height", size.ToString());

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
}
