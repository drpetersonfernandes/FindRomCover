using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using FindRomCover.models;
using Application = System.Windows.Application;
using Clipboard = System.Windows.Clipboard;
using Image = System.Windows.Controls.Image;
using MessageBox = System.Windows.MessageBox;

namespace FindRomCover;

public class ButtonFactory
{
    public static async Task<ObservableCollection<ImageData>> CreateSimilarImagesCollection(
        string selectedRomFileName,
        string imageFolderPath,
        double similarityThreshold,
        string similarityAlgorithm)
    {
        var similarImages = new ObservableCollection<ImageData>();

        // Logic to calculate similarity (mocked example)
        var images = await SimilarityCalculator.CalculateSimilarityAsync(
            selectedRomFileName,
            imageFolderPath,
            similarityThreshold,
            similarityAlgorithm
        ); // Ensure async behavior is properly awaited when used

        foreach (var image in images)
        {
            similarImages.Add(image);
        }

        return similarImages;
    }

    // Method to construct a context menu dynamically
    public static ContextMenu CreateContextMenu(string imagePath)
    {
        var contextMenu = new ContextMenu();

        // "Use This Image" menu item
        var useThisImageIcon = new Image
        {
            Source = new BitmapImage(new Uri("pack://application:,,,/images/usethis.png")),
            Width = 16,
            Height = 16,
            Margin = new Thickness(2)
        };
        var useThisImageMenuItem = new MenuItem
        {
            Header = "Use This Image",
            Command = UseThisImageCommand,
            CommandParameter = imagePath,
            Icon = useThisImageIcon
        };
        contextMenu.Items.Add(useThisImageMenuItem);

        // "Copy Image Filename" menu item
        var copyIcon = new Image
        {
            Source = new BitmapImage(new Uri("pack://application:,,,/images/copy.png")),
            Width = 16,
            Height = 16,
            Margin = new Thickness(2)
        };
        var copyMenuItem = new MenuItem
        {
            Header = "Copy Image Filename",
            Command = CopyImageFilenameCommand,
            CommandParameter = imagePath,
            Icon = copyIcon
        };
        contextMenu.Items.Add(copyMenuItem);

        // "Open File Location" menu item
        var openLocationIcon = new Image
        {
            Source = new BitmapImage(new Uri("pack://application:,,,/images/folder.png")),
            Width = 16,
            Height = 16,
            Margin = new Thickness(2)
        };
        var openLocationMenuItem = new MenuItem
        {
            Header = "Open File Location",
            Command = OpenFileLocationCommand,
            CommandParameter = imagePath,
            Icon = openLocationIcon
        };
        contextMenu.Items.Add(openLocationMenuItem);

        return contextMenu;
    }

    // Command for copying the image filename without extension
    private static ICommand CopyImageFilenameCommand { get; } = new DelegateCommand(param =>
    {
        if (param is not string imagePath) return;
        // Get filename without extension
        var filenameWithoutExtension = Path.GetFileNameWithoutExtension(imagePath);

        // Copy to clipboard
        Clipboard.SetText(filenameWithoutExtension);

        // Notify user
        MessageBox.Show($"Filename '{filenameWithoutExtension}' copied to clipboard!",
            "Copied", MessageBoxButton.OK, MessageBoxImage.Information);
    });

    private static ICommand OpenFileLocationCommand { get; } = new DelegateCommand(param =>
    {
        if (param is string imagePath && File.Exists(imagePath))
        {
            // Open the folder containing the file and select it
            System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{imagePath}\"");
        }
    });

    private static ICommand UseThisImageCommand { get; } = new DelegateCommand(param =>
    {
        if (param is not string imagePath) return;
        // Ensure that MainWindow's instance is available
        var mainWindow = Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();
        mainWindow?.UseImage(imagePath);
    });
}