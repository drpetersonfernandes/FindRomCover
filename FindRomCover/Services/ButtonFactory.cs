using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using FindRomCover.Models;
using Clipboard = System.Windows.Clipboard;
using ContextMenu = System.Windows.Controls.ContextMenu;
using Image = System.Windows.Controls.Image;
using MenuItem = System.Windows.Controls.MenuItem;
using MessageBox = System.Windows.MessageBox;

namespace FindRomCover.Services;

/// <summary>
/// Factory class for creating UI elements related to similar image suggestions.
/// Provides context menu creation and commands for image-related actions.
/// </summary>
/// <remarks>
/// This factory centralizes the creation of context menus for image suggestions,
/// ensuring consistent behavior and enabling caching of menu items for performance.
/// </remarks>
public static class ButtonFactory
{
    /// <summary>
    /// Creates a collection of similar images by calculating similarity scores between a ROM file name
    /// and all images in the specified folder.
    /// </summary>
    /// <param name="selectedRomFileName">The ROM file name to search for.</param>
    /// <param name="imageFolderPath">The path to the folder containing images.</param>
    /// <param name="similarityThreshold">The minimum similarity score (0-100) required for inclusion.</param>
    /// <param name="similarityAlgorithm">The algorithm to use for similarity calculation.</param>
    /// <param name="cancellationToken">A cancellation token to allow the operation to be cancelled.</param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> containing a <see cref="SimilarityCalculationResult"/> with similar images and any processing errors.
    /// </returns>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled via the cancellationToken.</exception>
    public static Task<SimilarityCalculationResult> CreateSimilarImagesCollection(
        string selectedRomFileName,
        string imageFolderPath,
        double similarityThreshold,
        string similarityAlgorithm,
        CancellationToken cancellationToken)
    {
        return SimilarityCalculator.CalculateSimilarityAsync(
            selectedRomFileName,
            imageFolderPath,
            similarityThreshold,
            similarityAlgorithm,
            cancellationToken);
    }

    /// <summary>
    /// Creates or updates a context menu for an image suggestion.
    /// </summary>
    /// <param name="imagePath">The full path to the image file.</param>
    /// <param name="useImageAction">The action to invoke when "Use This Image" is selected.</param>
    /// <param name="existingMenu">An existing context menu to reuse (optional).</param>
    /// <returns>
    /// A <see cref="ContextMenu"/> containing menu items for image operations.
    /// Returns an empty menu if imagePath is null or empty.
    /// </returns>
    /// <remarks>
    /// This method supports menu reuse for performance optimization. When an existing menu is provided,
    /// it updates the CommandParameter for all items instead of creating new menu items.
    /// 
    /// The context menu includes:
    /// - "Use This Image": Applies the image to the selected ROM
    /// - "Copy Image Filename": Copies the filename (without extension) to clipboard
    /// - "Open File Location": Opens Windows Explorer to the image's folder
    /// </remarks>
    public static ContextMenu CreateContextMenu(string imagePath, Action<string?> useImageAction, ContextMenu? existingMenu = null)
    {
        if (string.IsNullOrEmpty(imagePath))
        {
            const string errorMessage = "CreateContextMenu called with null or empty imagePath";
            var exception = new ArgumentException(errorMessage, nameof(imagePath));
            _ = ErrorLogger.LogAsync(exception, errorMessage); // Using fire-and-forget pattern to avoid blocking UI

            _ = ErrorLogger.LogAsync(exception, "Error creating context menu.");

            return new ContextMenu(); // Return a fresh empty menu
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
                Source = CreateFrozenBitmapImage("pack://application:,,,/images/usethis.png"),
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
            Source = CreateFrozenBitmapImage("pack://application:,,,/images/copy.png"),
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
            Source = CreateFrozenBitmapImage("pack://application:,,,/images/folder.png"),
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

    /// <summary>
    /// Gets a command that copies the image filename (without extension) to the clipboard.
    /// </summary>
    /// <remarks>
    /// This command handles COMException that can occur when the clipboard is locked by another process.
    /// </remarks>
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
            _ = ErrorLogger.LogAsync(ex, "Failed to set clipboard text.");
        }
    });

    /// <summary>
    /// Gets a command that opens Windows Explorer to the folder containing the image.
    /// </summary>
    /// <remarks>
    /// Uses ProcessStartInfo with explorer.exe and the /select parameter to highlight the file.
    /// </remarks>
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

    /// <summary>
    /// Creates a frozen (immutable) BitmapImage from a pack URI.
    /// </summary>
    /// <param name="uri">The pack URI to the image resource.</param>
    /// <returns>A frozen <see cref="BitmapImage"/>.</returns>
    /// <remarks>
    /// Freezing the bitmap improves performance and allows it to be used across multiple threads.
    /// </remarks>
    private static BitmapImage CreateFrozenBitmapImage(string uri)
    {
        var bitmap = new BitmapImage(new Uri(uri));
        if (bitmap.CanFreeze)
            bitmap.Freeze();

        return bitmap;
    }
}
