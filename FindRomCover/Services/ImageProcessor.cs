using System.IO;
using System.Windows;
using ImageMagick;

namespace FindRomCover.Services;

public static class ImageProcessor
{
    public sealed record ImageSaveResult(
        bool Success,
        string? ErrorMessage = null,
        string? ErrorTitle = null,
        MessageBoxImage ErrorIcon = MessageBoxImage.None,
        Exception? Exception = null,
        string? LogContext = null);

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
                catch (Exception ex)
                {
                    LogService.Warning(ex, $"Failed to delete orphaned temp file: {tempFile}");
                }
            }
        }
        catch (Exception ex)
        {
            LogService.Error(ex, $"Failed to enumerate temp files in directory: {directoryPath}");
        }
    }

    public static Task<ImageSaveResult> ConvertAndSaveImageAsync(string sourcePath, string? targetPath, CancellationToken cancellationToken)
    {
        if (targetPath != null) return ConvertAndSaveImageCoreAsync(sourcePath, targetPath, cancellationToken);

        return Task.FromResult(new ImageSaveResult(false, "Target path is null.", "Error", MessageBoxImage.Error));
    }

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

        const int maxAttempts = 3;
        for (var attempt = 0;; attempt++)
        {
            try
            {
                var testFile = Path.Combine(directory, $"{Guid.NewGuid()}.tmp");
                await File.WriteAllTextAsync(testFile, string.Empty, cancellationToken);
                File.Delete(testFile);
                break;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                if (attempt >= maxAttempts - 1)
                {
                    return new ImageSaveResult(
                        false,
                        $"Cannot write to directory: {directory}\n\nError: {ex.Message}\n\nTry running as administrator.",
                        "Permission Error",
                        MessageBoxImage.Error,
                        ex,
                        $"Cannot write to directory: {directory}");
                }
            }
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

        return await ProcessImageAsync(sourcePath, targetPath, cancellationToken);
    }

    private static async Task<ImageSaveResult> ProcessImageAsync(string sourcePath, string targetPath, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(sourcePath) || !File.Exists(sourcePath))
            return new ImageSaveResult(false, "Source image could not be found.", "Image Not Found", MessageBoxImage.Error);

        try
        {
            var settings = GetMagickReadSettings(sourcePath);
            using var magickImage = new MagickImage(sourcePath, settings);

            if (magickImage.Width == 0 || magickImage.Height == 0)
                throw new InvalidOperationException("Image has zero dimensions");

            magickImage.AutoOrient();
            magickImage.Quality = 90;
            magickImage.Format = MagickFormat.Png;

            return await WriteImageWithRetryAsync(magickImage, targetPath, sourcePath, cancellationToken);
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
                    if (File.Exists(tempPath))
                    {
                        try
                        {
                            File.Delete(tempPath);
                        }
                        catch (IOException)
                        {
                            // Temp file may be locked; will be retried
                        }
                        catch (UnauthorizedAccessException)
                        {
                            // Permission issue on temp file; will be retried
                        }
                    }

                    await magickImage.WriteAsync(tempPath, cancellationToken);

                    if (!File.Exists(tempPath))
                    {
                        throw new IOException("Failed to write temporary file");
                    }

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
                    var delay = baseDelayMs * Math.Pow(2, attempt - 1);
                    await Task.Delay((int)delay, cancellationToken);
                }
                catch (UnauthorizedAccessException ex) when (attempt < maxRetries)
                {
                    lastException = ex;
                    var delay = baseDelayMs * Math.Pow(2, attempt - 1);
                    await Task.Delay((int)delay, cancellationToken);
                }
                catch (MagickException ex) when (attempt < maxRetries && ex.Message.Contains("WriteBlob"))
                {
                    lastException = ex;
                    var delay = baseDelayMs * Math.Pow(2, attempt - 1);
                    await Task.Delay((int)delay, cancellationToken);
                }
                catch (Exception ex) when (attempt < maxRetries)
                {
                    lastException = ex;
                    var delay = baseDelayMs * Math.Pow(2, attempt - 1);
                    await Task.Delay((int)delay, cancellationToken);
                }
            }
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                try
                {
                    File.Delete(tempPath);
                }
                catch (IOException)
                {
                    // Best effort cleanup
                }
                catch (UnauthorizedAccessException)
                {
                    // Best effort cleanup
                }
            }
        }

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

    internal static MagickReadSettings GetMagickReadSettings(string filePath)
    {
        var settings = new MagickReadSettings();
        var ext = Path.GetExtension(filePath).ToLowerInvariant();

        settings.Format = ext switch
        {
            ".png" => MagickFormat.Png,
            ".jpg" or ".jpeg" => MagickFormat.Jpeg,
            ".bmp" => MagickFormat.Bmp,
            ".gif" => MagickFormat.Gif,
            ".tiff" or ".tif" => MagickFormat.Tiff,
            ".ico" => MagickFormat.Ico,
            ".svg" => MagickFormat.Svg,
            ".webp" => MagickFormat.WebP,
            ".avif" => MagickFormat.Avif,
            ".heic" => MagickFormat.Heic,
            ".heif" => MagickFormat.Heif,
            ".jxl" => MagickFormat.Jxl,
            ".jp2" => MagickFormat.Jp2,
            _ => MagickFormat.Unknown
        };

        return settings;
    }
}
