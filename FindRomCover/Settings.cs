using System.Globalization;
using System.IO;
using System.Xml;

namespace FindRomCover;

public class Settings
{
    private static readonly string SettingsFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.xml");

    public double SimilarityThreshold { get; set; } = 70;
    public string[] SupportedExtensions { get; private set; } = Array.Empty<string>();
    public int ImageWidth { get; set; } = 300;
    public int ImageHeight { get; set; } = 300;
    public string SelectedSimilarityAlgorithm { get; set; } = "Jaro-Winkler Distance";
    public string BaseTheme { get; set; } = "Light";
    public string AccentColor { get; set; } = "Blue";

    public Settings()
    {
        LoadSettings();
    }

    private void LoadSettings()
    {
        var doc = new XmlDocument();
        try
        {
            if (!File.Exists(SettingsFilePath))
            {
                SetDefaultSettings();
                SaveSettings(); // Save defaults immediately
                return;
            }

            doc.Load(SettingsFilePath);
            if (doc.DocumentElement?.Name != "Settings") throw new XmlException("Invalid root element in XML");

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
        catch (Exception ex)
        {
            MessageBox.Show("Error loading settings: " + ex.Message);
            SetDefaultSettings();
        }
    }

    public void SaveSettings()
    {
        var doc = new XmlDocument();
        try
        {
            if (File.Exists(SettingsFilePath))
            {
                doc.Load(SettingsFilePath);
            }
            else
            {
                var declaration = doc.CreateXmlDeclaration("1.0", "UTF-8", null);
                doc.AppendChild(declaration);

                var root = doc.CreateElement("Settings");
                doc.AppendChild(root);
            }

            // Save each setting
            SaveOrUpdateNode(doc, "SimilarityThreshold", SimilarityThreshold.ToString(CultureInfo.InvariantCulture));
            SaveSupportedExtensionsNode(doc); // Special handling for SupportedExtensions
            SaveImageSizeNode(doc); // Special handling for ImageSize
            SaveOrUpdateNode(doc, "BaseTheme", BaseTheme);
            SaveOrUpdateNode(doc, "AccentColor", AccentColor);
            SaveOrUpdateNode(doc, "SimilarityAlgorithm", SelectedSimilarityAlgorithm);

            doc.Save(SettingsFilePath);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Error saving settings: " + ex.Message);
        }
    }

    private void SaveSupportedExtensionsNode(XmlDocument doc)
    {
        var extensionsNode = doc.SelectSingleNode("//Settings/SupportedExtensions");
        if (extensionsNode == null)
        {
            extensionsNode = doc.CreateElement("SupportedExtensions");
            doc.DocumentElement?.AppendChild(extensionsNode);
        }
        else
        {
            extensionsNode.RemoveAll();
        }

        foreach (var extension in SupportedExtensions)
        {
            var extensionNode = doc.CreateElement("Extension");
            extensionNode.InnerText = extension;
            extensionsNode.AppendChild(extensionNode);
        }
    }

    private void SaveImageSizeNode(XmlDocument doc)
    {
        var imageSizeNode = doc.SelectSingleNode("//Settings/ImageSize");
        if (imageSizeNode == null)
        {
            imageSizeNode = doc.CreateElement("ImageSize");
            doc.DocumentElement?.AppendChild(imageSizeNode);
        }
        else
        {
            imageSizeNode.RemoveAll();
        }

        var widthNode = doc.CreateElement("Width");
        widthNode.InnerText = ImageWidth.ToString();
        imageSizeNode.AppendChild(widthNode);

        var heightNode = doc.CreateElement("Height");
        heightNode.InnerText = ImageHeight.ToString();
        imageSizeNode.AppendChild(heightNode);
    }

    private void SetDefaultSettings()
    {
        SimilarityThreshold = 70;
        SupportedExtensions = Array.Empty<string>();
        ImageWidth = 300;
        ImageHeight = 300;
        BaseTheme = "Light";
        AccentColor = "Blue";
    }
    
    private void SaveOrUpdateNode(XmlDocument doc, string key, string value)
    {
        var node = doc.SelectSingleNode($"//Settings/{key}");
        if (node == null)
        {
            node = doc.CreateElement(key);
            doc.DocumentElement?.AppendChild(node);
        }
        node.InnerText = value;
    }
}