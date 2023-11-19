using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace FindRomCover
{
    public partial class MainWindow : Window
    {
        private string? imageFolderPath;
        private string? selectedZipFileName;
        private readonly MediaPlayer _mediaPlayer = new();


        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
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
            System.Windows.MessageBox.Show("Find Rom Cover\nPeterson's Software\n11/2023", "About");
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
            string[] files = Directory.GetFiles(txtRomFolder.Text, "*.zip");
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
                selectedZipFileName = selectedFile; // Update the selected ZIP filename
                SimilarImages.Clear(); // Clear previous similar images

                if (!string.IsNullOrEmpty(imageFolderPath))
                {
                    string[] imageFiles = Directory.GetFiles(imageFolderPath, "*.png");
                    foreach (string imageFile in imageFiles)
                    {
                        string imageName = Path.GetFileNameWithoutExtension(imageFile);

                        if (IsSimilar(selectedZipFileName, imageName))
                        {
                            SimilarImages.Add(new ImageData
                            {
                                ImagePath = imageFile,
                                ImageName = imageName,
                                SimilarityRate = "50%" // Example similarity rate
                            });
                        }
                    }

                    // If no similar images found, add a "not found" message
                    if (SimilarImages.Count == 0)
                    {
                        SimilarImages.Add(new ImageData
                        {
                            ImageName = "There is no similar image",
                            IsNotFoundMessage = true
                        });
                    }
                }
            }
        }

        private static bool IsSimilar(string fileName, string otherFileName)
        {
            // Simple example: check if the other file name contains most of the characters of the original file name
            int similarityThreshold = (int)(fileName.Length * 0.5); // 50% similarity
            int matchCount = otherFileName.Where((c, i) => i < fileName.Length && c == fileName[i]).Count();

            return matchCount >= similarityThreshold;
        }

        public class ImageData
        {
            public string? ImagePath { get; set; }
            public string? ImageName { get; set; }
            public string? SimilarityRate { get; set; }
            public bool IsNotFoundMessage { get; set; } // Flag for "not found" message
                                                        // Any other properties...
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
                        LoadMissingImagesList();
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
    }
}
