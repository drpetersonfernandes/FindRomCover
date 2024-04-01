using System.IO;
using System.Xml;
using System.Windows;

namespace FindRomCover
{
    public class Settings
    {
        public double SimilarityThreshold { get; set; } = 30; // Default value
        public string[] SupportedExtensions { get; private set; } = Array.Empty<string>();
        public int ImageWidth { get; set; } = 300; // Default value
        public int ImageHeight { get; set; } = 300; // Default value
        public string SelectedSimilarityAlgorithm { get; private set; } = "Jaro-Winkler Distance"; // Default value

        public Settings()
        {
            LoadSettings();
        }

        private void LoadSettings()
        {
            var doc = new XmlDocument();
            try
            {
                doc.Load("settings.xml");

                // Load similarity rate
                var similarityNode = doc.SelectSingleNode("//Settings/SimilarityThreshold");
                if (similarityNode != null && double.TryParse(similarityNode.InnerText, out double savedRate))
                {
                    SimilarityThreshold = savedRate;
                }

                // Load supported extensions
                var extensionsNode = doc.SelectSingleNode("//Settings/SupportedExtensions");
                if (extensionsNode != null)
                {
                    var extensions = new List<string>();
                    foreach (XmlNode node in extensionsNode.ChildNodes)
                    {
                        extensions.Add(node.InnerText);
                    }
                    SupportedExtensions = extensions.ToArray();
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

                var algorithmNode = doc.SelectSingleNode("//Settings/SimilarityAlgorithm");
                if (algorithmNode != null)
                {
                    SelectedSimilarityAlgorithm = algorithmNode.InnerText;
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
                SetDefaultSettings();
            }
        }

        public void SaveSetting(string key, string value)
        {
            var doc = new XmlDocument();
            try
            {
                doc.Load("settings.xml");
            }
            catch (FileNotFoundException)
            {
                var declaration = doc.CreateXmlDeclaration("1.0", "UTF-8", null);
                doc.AppendChild(declaration);

                var root = doc.CreateElement("Settings");
                doc.AppendChild(root);
            }

            // Ensure the document has a root element before proceeding
            if (doc.DocumentElement == null)
            {
                // Handle the error appropriately. For simplicity, we'll just create a new root element.
                // In a real application, you might want to log this error or throw an exception.
                var root = doc.CreateElement("Settings");
                doc.AppendChild(root);
            }

            var node = doc.SelectSingleNode($"//Settings/{key}");
            if (node == null)
            {
                node = doc.CreateElement(key);
                if (doc.DocumentElement != null)
                {
                    doc.DocumentElement.AppendChild(node);
                }
                else
                {
                    // This is a fallback error handling and should technically never be reached due to the above check.
                    // Consider logging an error or throwing an exception.
                }
            }
            node.InnerText = value;
            doc.Save("settings.xml");
        }


        private void SetDefaultSettings()
        {
            SimilarityThreshold = 30;
            SupportedExtensions = Array.Empty<string>();
            ImageWidth = 300;
            ImageHeight = 300;
            SelectedSimilarityAlgorithm = "Jaro-Winkler Distance";
        }
    }
}
