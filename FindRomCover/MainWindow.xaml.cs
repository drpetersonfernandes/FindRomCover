using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml;

namespace FindRomCover
{
    public partial class MainWindow : Window
    {
        private string? imageFolderPath;
        private string? selectedRomFileName;
        private readonly MediaPlayer _mediaPlayer = new();
        private double similarityThreshold = 50;
        private string[]? supportedExtensions;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            LoadSettings();
        }

        public ObservableCollection<ImageData> SimilarImages { get; set; } = [];

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.MessageBox.Show("Find Rom Cover\nPure Logic Code\nVersion 1.1.0.2", "About");
        }

        private void BtnBrowseRomFolder_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new FolderBrowserDialog
            {
                // Set the description for the folder dialog
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
                // Set the description for the folder dialog
                Description = "Select the folder where your image files are stored."
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                txtImageFolder.Text = dialog.SelectedPath;
                imageFolderPath = dialog.SelectedPath; // Set the class-level variable
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

            // Use the loaded supportedExtensions array
            if (supportedExtensions == null || supportedExtensions.Length == 0)
            {
                System.Windows.MessageBox.Show("No supported file extensions loaded. Please check your settings.");
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
                await CalculateSimilarityAsync(selectedRomFileName);
            }
        }

        private async Task CalculateSimilarityAsync(string selectedFileName)
        {
            if (!string.IsNullOrEmpty(imageFolderPath))
            {
                string[] imageFiles = Directory.GetFiles(imageFolderPath, "*.png");
                List<ImageData> tempList = [];

                foreach (string imageFile in imageFiles)
                {
                    string imageName = Path.GetFileNameWithoutExtension(imageFile);
                    double similarityRate = await Task.Run(() => CalculateSimilarity(selectedFileName, imageName));

                    if (similarityRate >= similarityThreshold)
                    {
                        tempList.Add(new ImageData
                        {
                            ImagePath = imageFile,
                            ImageName = imageName,
                            SimilarityRate = similarityRate
                        });
                    }
                }

                // Sort the list by similarity rate in descending order
                tempList.Sort((x, y) => y.SimilarityRate.CompareTo(x.SimilarityRate));

                // Update the ObservableCollection on the UI thread
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    SimilarImages.Clear();
                    foreach (var item in tempList)
                    {
                        SimilarImages.Add(item);
                    }

                    if (SimilarImages.Count == 0)
                    {
                        SimilarImages.Add(new ImageData { ImageName = "No similar image found" });
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
                _mediaPlayer.Open(new Uri(soundPath, UriKind.Relative));
                _mediaPlayer.Play();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Error playing sound: " + ex.Message);
            }
        }

        public static double CalculateSimilarity(string a, string b)
        {
            if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return 0.0;

            int distance = LevenshteinDistance(a, b);
            double longestLength = Math.Max(a.Length, b.Length);
            double similarityRate = (1.0 - distance / longestLength) * 100;
            return Math.Round(similarityRate, 2); // Round to 2 decimal places
        }

        public static int LevenshteinDistance(string a, string b)
        {
            if (string.IsNullOrEmpty(a))
            {
                return b?.Length ?? 0;
            }

            if (string.IsNullOrEmpty(b))
            {
                return a.Length;
            }

            int lengthA = a.Length;
            int lengthB = b.Length;
            var distances = new int[lengthA + 1, lengthB + 1];

            for (int i = 0; i <= lengthA; distances[i, 0] = i++) { }
            for (int j = 0; j <= lengthB; distances[0, j] = j++) { }

            for (int i = 1; i <= lengthA; i++)
            {
                for (int j = 1; j <= lengthB; j++)
                {
                    int cost = (b[j - 1] == a[i - 1]) ? 0 : 1;
                    distances[i, j] = Math.Min(
                        Math.Min(distances[i - 1, j] + 1, distances[i, j - 1] + 1),
                        distances[i - 1, j - 1] + cost);
                }
            }
            return distances[lengthA, lengthB];
        }

        private void BtnRemoveSelectedItem_Click(object sender, RoutedEventArgs e)
        {
            RemoveSelectedItem();

        }

        private void RemoveSelectedItem()
        {
            if (lstMissingImages.SelectedItem != null)
            {
                lstMissingImages.Items.Remove(lstMissingImages.SelectedItem);
            }
        }

        private void SetSimilarityRate_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem clickedItem)
            {
                if (double.TryParse(clickedItem.Header.ToString(), out double rate))
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
                    if (double.TryParse(menuItem.Header.ToString(), out double rate))
                    {
                        menuItem.IsChecked = rate == similarityThreshold;
                    }
                }
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
                doc.DocumentElement?.AppendChild(newNode); // Use ?. to guard against null
            }
            doc.Save("settings.xml");
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
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Error loading settings: " + ex.Message);
                // Initialize with an empty array in case of exception
                supportedExtensions = [];
            }
        }

    }
}
