using System.IO;
using System.Windows;
using System.Xml;
using MessageBox = System.Windows.MessageBox;


namespace FindRomCover;

public class Settings
{
    public double SimilarityThreshold { get; set; } = 70;
    public string[] SupportedExtensions { get; private set; } = [];
    public int ImageWidth { get; private set; } = 300;
    public int ImageHeight { get; private set; } = 300;
    public string SelectedSimilarityAlgorithm { get; private set; } = "Jaro-Winkler Distance";

    // New properties for theme settings
    public string BaseTheme { get; private set; } = "Light";
    public string AccentColor { get; private set; } = "Blue";

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

            // Load thumbnail size
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

            // Load theme settings
            var baseThemeNode = doc.SelectSingleNode("//Settings/BaseTheme");
            if (baseThemeNode != null)
            {
                BaseTheme = baseThemeNode.InnerText;
            }

            var accentColorNode = doc.SelectSingleNode("//Settings/AccentColor");
            if (accentColorNode != null)
            {
                AccentColor = accentColorNode.InnerText;
            }

            var algorithmNode = doc.SelectSingleNode("//Settings/SimilarityAlgorithm");
            if (algorithmNode != null)
            {
                SelectedSimilarityAlgorithm = algorithmNode.InnerText;
            }
        }
        catch (FileNotFoundException)
        {
            MessageBox.Show("Settings file not found. Please ensure settings.xml is in the application directory.", "Settings File Missing", MessageBoxButton.OK, MessageBoxImage.Warning);
            SetDefaultSettings();
        }
        catch (Exception ex)
        {
            MessageBox.Show("Error loading settings: " + ex.Message);
            SetDefaultSettings();
        }
    }

    public static void SaveSetting(string key, string value)
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
            var root = doc.CreateElement("Settings");
            doc.AppendChild(root);
        }

        var node = doc.SelectSingleNode($"//Settings/{key}");
        if (node == null)
        {
            node = doc.CreateElement(key);
            doc.DocumentElement?.AppendChild(node);
        }
        node.InnerText = value;
        doc.Save("settings.xml");
    }

    private void SetDefaultSettings()
    {
        SimilarityThreshold = 70;
        SupportedExtensions = [];
        ImageWidth = 300;
        ImageHeight = 300;
        BaseTheme = "Light";
        AccentColor = "Blue";
    }
}