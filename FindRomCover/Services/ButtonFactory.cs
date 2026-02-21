using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using FindRomCover.models;
using Clipboard = System.Windows.Clipboard;
using ContextMenu = System.Windows.Controls.ContextMenu;
using Image = System.Windows.Controls.Image;
using MenuItem = System.Windows.Controls.MenuItem;
using MessageBox = System.Windows.MessageBox;

namespace FindRomCover.Services;

public static class ButtonFactory
{
    public static Task<SimilarityCalculationResult> CreateSimilarImagesCollection(
        string selectedRomFileName,
        string imageFolderPath,
        double similarityThreshold,
        string similarityAlgorithm,
        CancellationToken cancellationToken)
    {
        // Perform all work off-thread safely
        return Task.Run(async () =>
                await SimilarityCalculator.CalculateSimilarityAsync(
                    selectedRomFileName,
                    imageFolderPath,
                    similarityThreshold,
                    similarityAlgorithm,
                    cancellationToken
                ).ConfigureAwait(false),
            cancellationToken
        );
    }

    public static ContextMenu CreateContextMenu(string imagePath, Action<string?> useImageAction, ContextMenu? existingMenu = null)
    {
        if (string.IsNullOrEmpty(imagePath))
        {
            return existingMenu ?? new ContextMenu(); // Return existing or empty menu
        }

        // Reuse existing menu if provided and it has items (not empty)
        if (existingMenu is { Items.Count: > 0 })
        {
            // Update CommandParameter for all menu items to use the new imagePath
            foreach (var item in existingMenu.Items)
            {
                if (item is MenuItem menuItem)
                {
                    menuItem.CommandParameter = imagePath;
                }
            }

            return existingMenu;
        }

        var contextMenu = existingMenu ?? new ContextMenu();

        // Only add "Use This Image" menu item if the action is provided
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

        var filenameWithoutExtension = Path.GetFileNameWithoutExtension(imagePath);

        try
        {
            // Copy to clipboard
            Clipboard.SetText(filenameWithoutExtension);

            // Notify user
            MessageBox.Show($"Filename '{filenameWithoutExtension}' copied to clipboard!",
                "Copied", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (System.Runtime.InteropServices.COMException ex)
        {
            // The clipboard can be locked by other processes (e.g., remote desktop).
            MessageBox.Show("Could not copy to clipboard. It might be in use by another application.",
                "Clipboard Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            _ = ErrorLogger.LogAsync(ex, "Failed to set clipboard text due to COMException.");
        }
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