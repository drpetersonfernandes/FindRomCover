using System.IO;
using System.Windows;
using ImageMagick;

namespace FindRomCover.Services;

/// <summary>
/// Provides image processing functionality for converting, saving, and manipulating image files.
/// </summary>
/// <remarks>
/// This service uses the Magick.NET library for robust image processing capabilities.
/// It includes retry logic for handling file locking issues and atomic file operations
/// to prevent corruption during save operations.
/// </remarks>
public static class ImageProcessor
{
    /// <summary>
    /// Represents the result of an image save operation, containing success status and error details if applicable.
    /// </summary>
    /// <param name="Success">Indicates whether the save operation was successful.</param>
    /// <param name="ErrorMessage">The error message to display to the user if the operation failed.</param>
    /// <param name="ErrorTitle">The title for the error dialog.</param>
    /// <param name="ErrorIcon">The icon to display in the error dialog.</param>
    /// <param name="Exception">The exception that caused the failure, if any.</param>
    /// <param name="LogContext">Additional context information for logging purposes.</param>
    public sealed record ImageSaveResult(
        bool Success,
        string? ErrorMessage = null,
        string? ErrorTitle = null,
        MessageBoxImage ErrorIcon = MessageBoxImage.None,
        Exception? Exception = null,
        string? LogContext = null);

    /// <summary>
    /// Cleans up orphaned .tmp files from previous application crashes.
    /// Should be called on application startup for directories where images are saved.
    /// </summary>
    /// <param name="directoryPath">The directory to clean up.</param>
    public static void CleanupOrphanedTempFiles(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
            return;

        try
        {
            var tempFiles = Directory.GetFiles(directoryPath, "*.tmp");
            foreach (var tempFile in tempFiles)
            {
                try
                {
                    File.Delete(tempFile);
                }
                catch
                {
                    // Ignore errors - file may be in use by another process
                }
            }
        }
        catch
        {
            // Ignore errors - directory may not be accessible
        }
    }

    /// <summary>
    /// Converts an image file to PNG format and saves it to the specified target path asynchronously.
    /// </summary>
    /// <param name="sourcePath">The path to the source image file.</param>
    /// <param name="targetPath">The path where the converted PNG image should be saved.</param>
    /// <param name="cancellationToken">A cancellation token to allow the operation to be cancelled.</param>
    /// <returns>
    /// A <see cref="ImageSaveResult"/> containing the result of the operation.
    /// Success is true if the image was successfully converted and saved.
    /// </returns>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled via the cancellationToken.</exception>
    /// <remarks>
    /// This method performs several validation checks before processing:
    /// 1. Verifies source and target paths are different
    /// 2. Tests write permissions to the target directory
    /// 3. Handles existing file deletion with appropriate error messages
    /// 
    /// The conversion process:
    /// 1. Auto-orients the image based on EXIF data
    /// 2. Sets PNG format with 90% quality
    /// 3. Uses retry logic for handling file locking issues
    /// </remarks>
    public static Task<ImageSaveResult> ConvertAndSaveImageAsync(string sourcePath, string targetPath, CancellationToken cancellationToken)
    {
        return Task.Run(async () => await ConvertAndSaveImageCoreAsync(sourcePath, targetPath, cancellationToken), cancellationToken);
    }

    /// <summary>
    /// Core implementation for converting and saving an image.
    /// </summary>
    /// <param name="sourcePath">The path to the source image file.</param>
    /// <param name="targetPath">The target path for the converted image.</param>
    /// <param name="cancellationToken">A cancellation token to allow the operation to be cancelled.</param>
    /// <returns>A <see cref="ImageSaveResult"/> containing the result of the operation.</returns>
    private static async Task<ImageSaveResult> ConvertAndSaveImageCoreAsync(string sourcePath, string targetPath, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(targetPath);
        if (directory == null)
        {
            return new ImageSaveResult(false, "Invalid target path.", "Error", MessageBoxImage.Error);
        }

        if (sourcePath == targetPath)
        {
            return new ImageSaveResult(false, "Source and target paths are the same.\n\nPlease choose another target path.", "Error", MessageBoxImage.Error);
        }

        try
        {
            // Test if we can write to the directory
            var testFile = Path.Combine(directory, $"{Guid.NewGuid()}.tmp");
            await File.WriteAllTextAsync(testFile, string.Empty, cancellationToken).ConfigureAwait(false);
            File.Delete(testFile);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new ImageSaveResult(
                false,
                $"Cannot write to directory: {directory}\n\nError: {ex.Message}\n\nTry running as administrator.",
                "Permission Error",
                MessageBoxImage.Error,
                ex,
                $"Cannot write to directory: {directory}");
        }

        cancellationToken.ThrowIfCancellationRequested();

        if (File.Exists(targetPath))
        {
            try
            {
                File.Delete(targetPath);
            }
            catch (IOException ex)
            {
                return new ImageSaveResult(false,
                    $"The file '{Path.GetFileName(targetPath)}' is in use by another process.",
                    "File in Use",
                    MessageBoxImage.Error,
                    ex,
                    $"User cancelled retry for file in use: {targetPath}");
            }
            catch (UnauthorizedAccessException ex)
            {
                return new ImageSaveResult(false,
                    $"Access denied to file: {targetPath}\n\nTry running as administrator.",
                    "Permission Error",
                    MessageBoxImage.Error,
                    ex,
                    $"Access denied: {targetPath}");
            }
            catch (Exception ex)
            {
                return new ImageSaveResult(false,
                    $"Error deleting file: {ex.Message}",
                    "Error",
                    MessageBoxImage.Error,
                    ex,
                    $"Error deleting file: {targetPath}");
            }
        }

        cancellationToken.ThrowIfCancellationRequested();

        return await ProcessImageAsync(sourcePath, targetPath, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Processes the image using Magick.NET with proper validation.
    /// </summary>
    /// <param name="sourcePath">The source image path.</param>
    /// <param name="targetPath">The target path for the processed image.</param>
    /// <param name="cancellationToken">A cancellation token to allow the operation to be cancelled.</param>
    /// <returns>A <see cref="ImageSaveResult"/> containing the result of the operation.</returns>
    private static async Task<ImageSaveResult> ProcessImageAsync(string sourcePath, string targetPath, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(sourcePath) || !File.Exists(sourcePath))
            return new ImageSaveResult(false, "Source image could not be found.", "Image Not Found", MessageBoxImage.Error);

        try
        {
            // Use Magick.NET for robust image processing
            using var magickImage = new MagickImage(sourcePath);

            // Validate dimensions
            if (magickImage.Width == 0 || magickImage.Height == 0)
                throw new InvalidOperationException("Image has zero dimensions");

            // Auto-orient based on EXIF data
            magickImage.AutoOrient();

            // Set PNG compression and quality
            magickImage.Quality = 90;
            magickImage.Format = MagickFormat.Png;

            // Write to target path with retry logic for file locking issues
            return await WriteImageWithRetryAsync(magickImage, targetPath, sourcePath, cancellationToken).ConfigureAwait(false);
        }
        catch (MagickException ex)
        {
            return new ImageSaveResult(false,
                $"Error processing image with Magick.NET: {ex.Message}",
                "Image Processing Error",
                MessageBoxImage.Error,
                ex,
                $"Magick.NET error: {sourcePath}");
        }
    }

    /// <summary>
    /// Writes the image to the target path with retry logic for handling file locking issues.
    /// </summary>
    /// <param name="magickImage">The MagickImage to write.</param>
    /// <param name="targetPath">The target path for the image.</param>
    /// <param name="sourcePath">The source path (used for error messages).</param>
    /// <param name="cancellationToken">A cancellation token to allow the operation to be cancelled.</param>
    /// <returns>A <see cref="ImageSaveResult"/> indicating success or failure with details.</returns>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled via the cancellationToken.</exception>
    /// <remarks>
    /// This method implements exponential backoff retry logic to handle transient file locking
    /// issues commonly caused by antivirus software, cloud sync services (OneDrive), or
    /// other applications accessing the file.
    /// 
    /// The write process uses a temporary file followed by an atomic move operation to ensure
    /// that the target file is never in a partially written state.
    /// </remarks>
    private static async Task<ImageSaveResult> WriteImageWithRetryAsync(MagickImage magickImage, string targetPath, string sourcePath, CancellationToken cancellationToken)
    {
        const int maxRetries = 5;
        const int baseDelayMs = 100;
        Exception? lastException = null;
        var tempPath = targetPath + ".tmp";

        try
        {
            for (var attempt = 1; attempt <= maxRetries; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    // Clean up any leftover temp file from previous attempts
                    if (File.Exists(tempPath))
                    {
                        try
                        {
                            File.Delete(tempPath);
                        }
                        catch
                        {
                            /* ignore - will retry or clean up at end */
                        }
                    }

                    // Write to temporary file first
                    await magickImage.WriteAsync(tempPath, cancellationToken).ConfigureAwait(false);

                    // Ensure the file was written successfully
                    if (!File.Exists(tempPath))
                    {
                        throw new IOException("Failed to write temporary file");
                    }

                    // Move temp file to target with overwrite (atomic replace operation)
                    // Using overwrite parameter ensures atomicity - either the operation succeeds
                    // and the new file is in place, or it fails and the original remains
                    File.Move(tempPath, targetPath, true);

                    return File.Exists(targetPath)
                        ? new ImageSaveResult(true)
                        : new ImageSaveResult(false,
                            "The image was processed but the target file was not created.",
                            "File Write Error",
                            MessageBoxImage.Error,
                            new IOException("Target file was not created"),
                            $"WriteBlob failed for: {sourcePath}");
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (IOException ex) when (attempt < maxRetries)
                {
                    lastException = ex;
                    // Calculate delay with exponential backoff (100ms, 200ms, 400ms, 800ms, 1600ms)
                    var delay = baseDelayMs * Math.Pow(2, attempt - 1);
                    await Task.Delay((int)delay, cancellationToken).ConfigureAwait(false);
                }
                catch (UnauthorizedAccessException ex) when (attempt < maxRetries)
                {
                    lastException = ex;
                    // Retry on permission issues (may be caused by antivirus/OneDrive)
                    var delay = baseDelayMs * Math.Pow(2, attempt - 1);
                    await Task.Delay((int)delay, cancellationToken).ConfigureAwait(false);
                }
                catch (MagickException ex) when (attempt < maxRetries && ex.Message.Contains("WriteBlob"))
                {
                    lastException = ex;
                    // Retry on WriteBlob errors specifically
                    var delay = baseDelayMs * Math.Pow(2, attempt - 1);
                    await Task.Delay((int)delay, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex) when (attempt < maxRetries)
                {
                    lastException = ex;
                    // Catch any other exceptions and retry
                    var delay = baseDelayMs * Math.Pow(2, attempt - 1);
                    await Task.Delay((int)delay, cancellationToken).ConfigureAwait(false);
                }
            }
        }
        finally
        {
            // Final cleanup attempt: ensure temp file is deleted after all retries
            // This handles cases where File.Move failed after partial write, or file was locked
            if (File.Exists(tempPath))
            {
                try
                {
                    File.Delete(tempPath);
                }
                catch (Exception cleanupEx)
                {
                    // Log the cleanup failure but don't throw - will be cleaned up on next startup
                    _ = ErrorLogger.LogAsync(cleanupEx, $"Failed to cleanup temp file: {tempPath}");
                }
            }
        }

        // All retries exhausted - log the error and show message
        var errorMessage = $"Failed to save image after {maxRetries} attempts. The file may be locked by another process (e.g., OneDrive sync, antivirus).\n\nTarget: {targetPath}";
        if (lastException != null)
        {
            errorMessage += $"\n\nLast error: {lastException.Message}";
        }

        return new ImageSaveResult(
            false,
            errorMessage,
            "File Write Error",
            MessageBoxImage.Error,
            lastException ?? new IOException("Write failed after all retries"),
            $"WriteBlob failed after {maxRetries} retries for: {sourcePath}");
    }
}
