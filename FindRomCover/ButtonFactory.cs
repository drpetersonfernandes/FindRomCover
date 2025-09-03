using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using FindRomCover.models;
using Clipboard = System.Windows.Clipboard;
using Image = System.Windows.Controls.Image;
using MessageBox = System.Windows.MessageBox;

namespace FindRomCover;

public class ButtonFactory
{
    public static async Task<List<ImageData>> CreateSimilarImagesCollection(
        string selectedRomFileName,
        string imageFolderPath,
        double similarityThreshold,
        string similarityAlgorithm,
        CancellationToken cancellationToken)
    {
        // Perform all work off-thread safely
        var images = await SimilarityCalculator.CalculateSimilarityAsync(
            selectedRomFileName,
            imageFolderPath,
            similarityThreshold,
            similarityAlgorithm,
            cancellationToken
        );

        // Return plain list — safe for cross-thread use
        return cancellationToken.IsCancellationRequested
            ? []
            : images;
    }

    public static ContextMenu CreateContextMenu(string imagePath, Action<string?> useImageAction)
    {
        if (string.IsNullOrEmpty(imagePath))
        {
            return new ContextMenu(); // Return empty menu
        }

        var contextMenu = new ContextMenu();

        // Only add "Use This Image" menu item if the action is provided
        if (useImageAction != null)
        {
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
                Command = new DelegateCommand(p => useImageAction.Invoke(p as string)),
                CommandParameter = imagePath,
                Icon = useThisImageIcon
            };
            contextMenu.Items.Add(useThisImageMenuItem);
        }

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

    private static ICommand OpenFileLocationCommand { get; } = new DelegateCommand(static param =>
    {
        if (param is not string imagePath || string.IsNullOrEmpty(imagePath))
        {
            MessageBox.Show("Image path is null or empty.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        if (File.Exists(imagePath))
        {
            // Open the folder containing the file and select it using ProcessStartInfo for safer execution
            var processStartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"/select,\"{imagePath}\"",
                UseShellExecute = true
            };
            System.Diagnostics.Process.Start(processStartInfo);
        }
    });
}
