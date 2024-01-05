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
        private string? selectedZipFileName;
        private readonly MediaPlayer _mediaPlayer = new();
        private double similarityThreshold = 60;  // default value

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            // Load settings
            LoadSettings();
        }

        //Collection that will hold the data for the similar images
        public ObservableCollection<ImageData> SimilarImages { get; set; } = [];

        //Exit_Click event handler
        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        //About_Click event handler
        private void About_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.MessageBox.Show("Find Rom Cover\nPeterson's Software\nVersion 1.1\n01/2024", "About");
        }

        private void BtnBrowseRomFolder_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new FolderBrowserDialog();
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                txtRomFolder.Text = dialog.SelectedPath;
            }
        }

        private void BtnBrowseImageFolder_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new FolderBrowserDialog();
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

        private void CheckForMissingImages(string[] zipFiles)
        {
            lstMissingImages.Items.Clear();

            foreach (string file in zipFiles)
            {
                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(file);
                string correspondingImage = Path.Combine(txtImageFolder.Text, fileNameWithoutExtension + ".png");

                if (!File.Exists(correspondingImage))
                {
                    lstMissingImages.Items.Add(fileNameWithoutExtension);
                }
            }
        }

        private void LoadMissingImagesList()
        {
            if (string.IsNullOrEmpty(txtRomFolder.Text) || string.IsNullOrEmpty(txtImageFolder.Text))
            {
                System.Windows.MessageBox.Show("Please select both ROM and Image folders.");
                return;
            }

            lstMissingImages.Items.Clear();

            string[] supportedExtensions = ["*.zip", "*.7z", "*.cdi", "*.chd", "*.iso", "*.3ds", "*.rvz", "*.nsp", "*.xci", "*.wua", "*.wad", "*.cso"];

            // Get all files in the directory with supported extensions
            var files = supportedExtensions.SelectMany(ext => Directory.GetFiles(txtRomFolder.Text, ext)).ToArray();

            foreach (string file in files)
            {
                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(file);
                string correspondingImage = Path.Combine(txtImageFolder.Text, fileNameWithoutExtension + ".png");

                if (!File.Exists(correspondingImage))
                {
                    lstMissingImages.Items.Add(fileNameWithoutExtension);
                }
            }
        }

        private void LstMissingImages_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstMissingImages.SelectedItem is string selectedFile)
            {
                selectedZipFileName = selectedFile;
                List<ImageData> tempList = [];

                if (!string.IsNullOrEmpty(imageFolderPath))
                {
                    string[] imageFiles = Directory.GetFiles(imageFolderPath, "*.png");
                    foreach (string imageFile in imageFiles)
                    {
                        string imageName = Path.GetFileNameWithoutExtension(imageFile);
                        double similarityRate = CalculateSimilarity(selectedZipFileName, imageName);

                        if (similarityRate >= similarityThreshold) // Use the variable here
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

                    // Clear and add sorted items to SimilarImages
                    SimilarImages.Clear();
                    foreach (var item in tempList)
                    {
                        SimilarImages.Add(item);
                    }

                    if (SimilarImages.Count == 0)
                    {
                        // No similar images found, add not found message
                        SimilarImages.Add(new ImageData { ImageName = "No similar image found" });
                    }
                }
            }
        }

        public class ImageData
        {
            public string? ImagePath { get; set; }
            public string? ImageName { get; set; }
            public double SimilarityRate { get; set; }
            public bool IsNotFoundMessage { get; set; } // Flag for "not found" message
        }

        private void ImageCell_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement selectedCell && selectedCell.DataContext is ImageData imageData)
            {
                if (!string.IsNullOrEmpty(selectedZipFileName) &&
                    !string.IsNullOrEmpty(imageData.ImagePath) &&
                    !string.IsNullOrEmpty(imageFolderPath))
                {
                    string newFileName = Path.Combine(imageFolderPath, selectedZipFileName + ".png");
                    try
                    {
                        File.Copy(imageData.ImagePath, newFileName, true);
                        // Play click sound
                        PlayClickSound();
                        // Refresh the missing images list
                        // LoadMissingImagesList();
                        // Remove the selected item from the list
                        RemoveSelectedItem();
                        // Clear the bound collection
                        SimilarImages.Clear();
                    }
                    catch (Exception ex)
                    {
                        System.Windows.MessageBox.Show("Error copying file: " + ex.Message);
                    }
                }
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
            doc.Load("settings.xml");
            var node = doc.SelectSingleNode("//Settings/SimilarityRate");
            if (node != null && double.TryParse(node.InnerText, out double savedRate))
            {
                similarityThreshold = savedRate;
                UncheckAllSimilarityRates(); // Make sure to reflect this in the UI
            }
        }

    }
}
