using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml;
using System.ComponentModel;

namespace FindRomCover
{
    public partial class MainWindow : Window
    {
        private string? imageFolderPath;
        private string? selectedRomFileName;
        private readonly MediaPlayer _mediaPlayer = new();
        private double similarityThreshold;
        private string[]? supportedExtensions;
        private int _imageWidth = 300;
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

        private int _imageHeight = 300;
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

        public event PropertyChangedEventHandler PropertyChanged = null!;
        protected virtual void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            LoadSettings();
            UpdateThumbnailSizeMenuChecks();

        }

        public ObservableCollection<ImageData> SimilarImages { get; set; } = [];

        private void About_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.MessageBox.Show("Find Rom Cover\nPure Logic Code\nVersion 1.2.0.3", "About");
        }

        private void SetThumbnailSize_Click(object sender, RoutedEventArgs e)
        {

            if (sender is MenuItem menuItem && menuItem.Header != null && int.TryParse(menuItem.Header.ToString()!.Split(' ')[0], out int size))
            {
                // Update properties
                ImageWidth = size;
                ImageHeight = size;

                // Save the new size to settings.xml
                SaveSetting("ImageSize/Width", size.ToString());
                SaveSetting("ImageSize/Height", size.ToString());

                // Ensure only the selected menu item is checked
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
                    if (int.TryParse(menuItem.Header.ToString()!.Split(' ')[0], out int size))
                    {
                        menuItem.IsChecked = size == currentSize;
                    }
                }
            }
        }

        private void DonateButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "https://www.buymeacoffee.com/purelogiccode",
                    UseShellExecute = true
                };
                System.Diagnostics.Process.Start(psi);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Unable to open the link: " + ex.Message);
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
                txtRomFolder.Text = dialog.SelectedPath;
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
                txtImageFolder.Text = dialog.SelectedPath;
                imageFolderPath = dialog.SelectedPath;
            }
        }

        private void BtnCheckForMissingImages_Click(object sender, RoutedEventArgs e)
        {
            LoadMissingImagesList();
        }

        private void CheckForMissingImages(string[] romFiles)
        {
            foreach (string file in romFiles)
            {
                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(file);

                string? correspondingImagePath = FindCorrespondingImage(fileNameWithoutExtension);

                if (correspondingImagePath == null)
                {
                    lstMissingImages.Items.Add(fileNameWithoutExtension);
                }
            }
        }

        private string? FindCorrespondingImage(string fileNameWithoutExtension)
        {
            string[] imageExtensions = [".png", ".jpg", ".jpeg"];
            foreach (var ext in imageExtensions)
            {
                string imagePath = Path.Combine(txtImageFolder.Text, fileNameWithoutExtension + ext);
                if (File.Exists(imagePath))
                {
                    return imagePath;
                }
            }
            return null;
        }

        private void LoadMissingImagesList()
        {
            if (supportedExtensions == null || supportedExtensions.Length == 0)
            {
                System.Windows.MessageBox.Show("No supported file extensions loaded.");
                return;
            }

            if (string.IsNullOrEmpty(txtRomFolder.Text) || string.IsNullOrEmpty(txtImageFolder.Text))
            {
                System.Windows.MessageBox.Show("Please select both ROM and Image folders.");
                return;
            }

            lstMissingImages.Items.Clear();

            if (supportedExtensions == null || supportedExtensions.Length == 0)
            {
                System.Windows.MessageBox.Show("No supported file extensions loaded. Please check your settings.xml.");
                return;
            }

            // Prepend wildcard and dot to each extension
            var searchPatterns = supportedExtensions.Select(ext => "*." + ext).ToArray();

            // Get all files in the directory with supported extensions
            var files = searchPatterns.SelectMany(ext => Directory.GetFiles(txtRomFolder.Text, ext)).ToArray();

            // Call CheckForMissingImages with the found files
            CheckForMissingImages(files);
        }

        private async void LstMissingImages_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstMissingImages.SelectedItem is string selectedFile)
            {
                selectedRomFileName = selectedFile;
                var imageFolderPath = this.imageFolderPath; // Ensure this is the path to your images
                var similarityThreshold = this.similarityThreshold; // Ensure this is set correctly

                // Call the method and await its result
                var similarImages = await SimilarityCalculator.CalculateSimilarityAsync(selectedFile, imageFolderPath!, similarityThreshold, SelectedSimilarityAlgorithm);

                // Update the UI accordingly
                // Assuming SimilarImages is an ObservableCollection bound to a UI control
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    // Update the label to display the search query
                    lblSearchQuery.Content = "Similarity Algorithm: " + SelectedSimilarityAlgorithm + "\nSearch Query: " + selectedFile;

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
            public string? ImagePath { get; set; }
            public string? ImageName { get; set; }
            public double SimilarityRate { get; set; }
            public bool IsNotFoundMessage { get; set; }
        }

        private void ImageCell_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement selectedCell && selectedCell.DataContext is ImageData imageData)
            {
                if (!string.IsNullOrEmpty(selectedRomFileName) &&
                    !string.IsNullOrEmpty(imageData.ImagePath) &&
                    !string.IsNullOrEmpty(imageFolderPath))
                {
                    string newFileName = Path.Combine(imageFolderPath, selectedRomFileName + ".png");
                    if (ConvertAndSaveImage(imageData.ImagePath, newFileName))
                    {
                        PlayClickSound();
                        RemoveSelectedItem();
                        SimilarImages.Clear();
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
                    using var bitmap = new System.Drawing.Bitmap(image);
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

        private void PlayClickSound()
        {
            try
            {
                string soundPath = "audio/click.mp3";
                _mediaPlayer.MediaOpened += (sender, e) => {
                    _mediaPlayer.Play();
                };
                _mediaPlayer.Open(new Uri(soundPath, UriKind.Relative));
                _mediaPlayer.Volume = 1.0; // Maximum volume
                _mediaPlayer.Play();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Error playing sound: " + ex.Message);
            }
        }

        private void BtnRemoveSelectedItem_Click(object sender, RoutedEventArgs e)
        {
            RemoveSelectedItem();
            PlayClickSound();

        }

        private void RemoveSelectedItem()
        {
            if (lstMissingImages.SelectedItem != null)
            {
                lstMissingImages.Items.Remove(lstMissingImages.SelectedItem);
            }
        }

        private void SetSimilarityThreshold_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem clickedItem)
            {
                // Remove the '%' symbol before parsing
                string headerText = clickedItem.Header.ToString()!.Replace("%", "");

                if (double.TryParse(headerText, out double rate))
                {
                    similarityThreshold = rate;
                    UncheckAllSimilarityRates();
                    clickedItem.IsChecked = true;
                    // Save to Settings.xml
                    SaveSetting("SimilarityRate", similarityThreshold.ToString());
                }
                else
                {
                    System.Windows.MessageBox.Show("Invalid similarity rate selected.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void UncheckAllSimilarityRates()
        {
            foreach (var item in MySimilarityMenu.Items)
            {
                if (item is MenuItem menuItem)
                {
                    if (double.TryParse(menuItem.Header.ToString()!.Replace("%", ""), out double rate))
                    {
                        menuItem.IsChecked = Math.Abs(rate - similarityThreshold) < 0.01; // Checking for equality in double
                    }
                }
            }
        }

        private void LoadSettings()
        {

            var doc = new XmlDocument();
            try
            {
                doc.Load("settings.xml");

                // Load similarity rate
                var similarityNode = doc.SelectSingleNode("//Settings/SimilarityRate");
                if (similarityNode != null && double.TryParse(similarityNode.InnerText, out double savedRate))
                {
                    similarityThreshold = savedRate;
                    UncheckAllSimilarityRates();
                }
                else
                {
                    similarityThreshold = 30; // Default value if not found in settings
                }

                // Load supported extensions
                var extensionsNode = doc.SelectSingleNode("//Settings/SupportedExtensions");
                if (extensionsNode != null)
                {
                    var extensionNodes = extensionsNode.SelectNodes("./Extension");
                    var extensions = new List<string>();

                    if (extensionNodes != null)
                    {
                        foreach (XmlNode node in extensionNodes)
                        {
                            if (node.InnerText != null)
                            {
                                extensions.Add(node.InnerText);
                            }
                        }
                    }

                    supportedExtensions = [.. extensions];
                }
                else
                {
                    // If no extensions are found, use an empty array
                    supportedExtensions = [];
                }

                // Load image size
                var imageSizeNode = doc.SelectSingleNode("//Settings/ImageSize");
                if (imageSizeNode != null)
                {
                    var widthNode = imageSizeNode.SelectSingleNode("Width");
                    var heightNode = imageSizeNode.SelectSingleNode("Height");

                    if (widthNode != null && int.TryParse(widthNode.InnerText, out int width))
                    {
                        ImageWidth = width;
                    }
                    if (heightNode != null && int.TryParse(heightNode.InnerText, out int height))
                    {
                        ImageHeight = height;
                    }
                }

            }
            catch (FileNotFoundException)
            {
                System.Windows.MessageBox.Show("Settings file not found. Please ensure settings.xml is in the application directory.", "Settings File Missing", MessageBoxButton.OK, MessageBoxImage.Warning);
                SetDefaultSettings();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Error loading settings: " + ex.Message);
                supportedExtensions = [];
                similarityThreshold = 30;
            }
        }

        private static void SaveSetting(string key, string value)
        {
            var doc = new XmlDocument();
            try
            {
                doc.Load("settings.xml");
            }
            catch (FileNotFoundException)
            {
                // File not found, create a new XML document with a root element
                var declaration = doc.CreateXmlDeclaration("1.0", "utf-8", null);
                doc.AppendChild(declaration);

                var root = doc.CreateElement("Settings");
                doc.AppendChild(root);
            }
            catch (XmlException)
            {
                // Handle cases where the file is not well-formed XML
                System.Windows.MessageBox.Show("The settings file is not well-formed XML. Please delete it and restart the application.");
            }

            var node = doc.SelectSingleNode($"//Settings/{key}");
            if (node != null)
            {
                node.InnerText = value;
            }
            else
            {
                var newNode = doc.CreateElement(key);
                newNode.InnerText = value;
                doc.DocumentElement?.AppendChild(newNode);
            }
            doc.Save("settings.xml");
        }

        private void SetDefaultSettings()
        {
            similarityThreshold = 30;
            supportedExtensions = [];
            ImageWidth = 300;
            ImageHeight = 300;
            UncheckAllSimilarityRates();
        }

        public string SelectedSimilarityAlgorithm { get; set; } = "Jaro-Winkler Distance"; // Default value

        private void SetSimilarityAlgorithm_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem)
            {
                SelectedSimilarityAlgorithm = menuItem.Header.ToString()!;
                UncheckAllSimilarityAlgorithms();
                menuItem.IsChecked = true;
            }
        }

        private void UncheckAllSimilarityAlgorithms()
        {
            MenuAlgorithmJaccard.IsChecked = SelectedSimilarityAlgorithm == "Jaccard Similarity";
            MenuAlgorithmJaroWinkler.IsChecked = SelectedSimilarityAlgorithm == "Jaro-Winkler Distance";
            MenuAlgorithmLevenshtein.IsChecked = SelectedSimilarityAlgorithm == "Levenshtein Distance";
        }


    }
}
