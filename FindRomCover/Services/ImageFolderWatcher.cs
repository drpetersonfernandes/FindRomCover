using System.Collections.Concurrent;
using System.IO;
using ImageMagick;

namespace FindRomCover.Services;

public sealed class ImageFolderWatcher : IDisposable
{
    private FileSystemWatcher? _watcher;
    private readonly SemaphoreSlim _processingLock = new(1, 1);
    private readonly ConcurrentDictionary<string, byte> _recentlyProcessed = new();
    private readonly CancellationTokenSource _disposeCts = new();
    private bool _disposed;

    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".bmp", ".gif", ".webp", ".tiff", ".tif",
        ".avif", ".heic", ".heif", ".ico", ".svg", ".jxl", ".jp2"
    };

    public event Action<string>? ImageFound;
    public event Action<string, string>? ConversionFailed;

    private string? _pendingRenameTarget;
    private readonly object _renameLock = new();

    public string? PendingRenameTarget
    {
        get { lock (_renameLock) { return _pendingRenameTarget; } }
        set
        {
            lock (_renameLock)
            {
                var old = _pendingRenameTarget;
                _pendingRenameTarget = value;
                if (old != value)
                    LogService.Debug($"ImageFolderWatcher: PendingRenameTarget changed from '{old}' to '{value}'");
            }
        }
    }

    private void TryClearPendingRenameTarget(string? expectedValue)
    {
        lock (_renameLock)
        {
            if (_pendingRenameTarget != expectedValue) return;

            var old = _pendingRenameTarget;
            _pendingRenameTarget = null;
            LogService.Debug($"ImageFolderWatcher: PendingRenameTarget cleared from '{old}'");
        }
    }

    public void PreRegisterExpectedFile(string filePath)
    {
        _recentlyProcessed.TryAdd(filePath, 1);
        _ = Task.Run(async () =>
        {
            try { await Task.Delay(60000, _disposeCts.Token); } catch (OperationCanceledException) { }
            _recentlyProcessed.TryRemove(filePath, out _);
        });
        LogService.Debug($"ImageFolderWatcher: pre-registered '{Path.GetFileName(filePath)}' so watcher will skip it");
    }

    public void Start(string folderPath)
    {
        Stop();

        if (!Directory.Exists(folderPath))
        {
            LogService.Warning($"ImageFolderWatcher: folder does not exist: {folderPath}");
            return;
        }

        _watcher = new FileSystemWatcher(folderPath)
        {
            NotifyFilter = NotifyFilters.FileName,
            Filter = "*.*",
            IncludeSubdirectories = false,
            InternalBufferSize = 64 * 1024,
            EnableRaisingEvents = true
        };

        _watcher.Created += OnFileCreatedAsync;
        _watcher.Renamed += OnFileRenamedAsync;
        _watcher.Error += OnWatcherError;

        LogService.Information($"ImageFolderWatcher: started watching '{folderPath}'");
    }

    public void Stop()
    {
        if (_watcher == null) return;

        _watcher.EnableRaisingEvents = false;
        _watcher.Created -= OnFileCreatedAsync;
        _watcher.Renamed -= OnFileRenamedAsync;
        _watcher.Error -= OnWatcherError;
        _watcher.Dispose();
        _watcher = null;

        // Wait for any in-flight ProcessFileAsync to complete
        if (!_processingLock.Wait(TimeSpan.FromSeconds(15)))
        {
            LogService.Warning("ImageFolderWatcher: timed out waiting for in-flight processing to complete");
        }
        else
        {
            _processingLock.Release();
        }

        LogService.Information("ImageFolderWatcher: stopped");
    }

    private static void OnWatcherError(object sender, ErrorEventArgs e)
    {
        LogService.Error(e.GetException(), "ImageFolderWatcher: FileSystemWatcher error (buffer overflow or system error)");
    }

    private async void OnFileCreatedAsync(object sender, FileSystemEventArgs e)
    {
        try
        {
            if (_disposed) return;

            LogService.Debug($"ImageFolderWatcher: OnFileCreated fired for '{e.FullPath}'");

            try
            {
                await ProcessFileAsync(e.FullPath);
            }
            catch (Exception ex)
            {
                LogService.Error(ex, $"ImageFolderWatcher: unhandled error in OnFileCreated for '{e.FullPath}'");
            }
        }
        catch (Exception ex)
        {
            LogService.Error(ex, "Error in method OnFileCreatedAsync");
        }
    }

    private async void OnFileRenamedAsync(object sender, RenamedEventArgs e)
    {
        try
        {
            if (_disposed) return;

            LogService.Debug($"ImageFolderWatcher: OnFileRenamed fired for '{e.FullPath}'");

            try
            {
                await ProcessFileAsync(e.FullPath);
            }
            catch (Exception ex)
            {
                LogService.Error(ex, $"ImageFolderWatcher: unhandled error in OnFileRenamed for '{e.FullPath}'");
            }
        }
        catch (Exception ex)
        {
            LogService.Error(ex, "Error in method OnFileRenamedAsync");
        }
    }

    private async Task ProcessFileAsync(string filePath)
    {
        try
        {
            if (_disposed) return;

            var extension = Path.GetExtension(filePath).ToLowerInvariant();

            if (!SupportedExtensions.Contains(extension))
            {
                LogService.Debug($"ImageFolderWatcher: skipping '{Path.GetFileName(filePath)}' — extension '{extension}' not supported");
                return;
            }

            var fileNameWithoutExt = Path.GetFileNameWithoutExtension(filePath);

            if (string.IsNullOrEmpty(fileNameWithoutExt))
                return;

            await _processingLock.WaitAsync();
            try
            {
                // Deduplicate INSIDE the lock to prevent races with file system events
                // that fire during our own rename/convert operations
                if (!_recentlyProcessed.TryAdd(filePath, 1))
                {
                    LogService.Debug($"ImageFolderWatcher: skipping '{Path.GetFileName(filePath)}' — dedup hit");
                    return;
                }

                // Clean up old dedupe entries after 60 seconds (must outlast any in-lock wait)
                var dedupeKey = filePath;
                _ = Task.Run(async () =>
                {
                    try { await Task.Delay(60000, _disposeCts.Token); } catch (OperationCanceledException) { }
                    _recentlyProcessed.TryRemove(dedupeKey, out _);
                });

                await WaitForFileReadyAsync(filePath);

                if (!File.Exists(filePath))
                {
                    _recentlyProcessed.TryRemove(filePath, out _);

                    var pngEquivalent = Path.Combine(
                        Path.GetDirectoryName(filePath) ?? ".",
                        Path.GetFileNameWithoutExtension(filePath) + ".png");

                    if (File.Exists(pngEquivalent))
                    {
                        LogService.Debug($"ImageFolderWatcher: source file gone after PNG conversion: '{Path.GetFileName(filePath)}'");
                    }
                    else
                    {
                        LogService.Debug($"ImageFolderWatcher: file disappeared after wait: '{filePath}'");
                    }

                    return;
                }

                var renameTarget = PendingRenameTarget;
                LogService.Debug($"ImageFolderWatcher: processing '{Path.GetFileName(filePath)}' — PendingRenameTarget = '{renameTarget}'");

                var wasRenamed = false;
                if (!string.IsNullOrEmpty(renameTarget))
                {
                    var directory = Path.GetDirectoryName(filePath);
                    var renamedPath = Path.Combine(directory ?? ".", renameTarget + extension);

                    if (string.Equals(filePath, renamedPath, StringComparison.OrdinalIgnoreCase))
                    {
                        LogService.Debug($"ImageFolderWatcher: source and target are the same ('{Path.GetFileName(filePath)}'), skipping rename");
                        wasRenamed = true;
                        TryClearPendingRenameTarget(renameTarget);
                    }
                    else
                    {
                        if (File.Exists(renamedPath))
                        {
                            LogService.Debug($"ImageFolderWatcher: target '{Path.GetFileName(renamedPath)}' already exists, deleting it first");
                            try { File.Delete(renamedPath); }
                            catch (Exception ex)
                            {
                                LogService.Error(ex, $"ImageFolderWatcher: failed to delete existing target '{renamedPath}'");
                            }
                        }

                        var renamed = await MoveFileWithRetryAsync(filePath, renamedPath);
                        if (renamed)
                        {
                            wasRenamed = true;
                            TryClearPendingRenameTarget(renameTarget);
                            _recentlyProcessed.TryRemove(filePath, out _);
                            _recentlyProcessed.TryAdd(renamedPath, 1);
                            _ = Task.Run(async () =>
                            {
                                try { await Task.Delay(60000, _disposeCts.Token); } catch (OperationCanceledException) { }
                                _recentlyProcessed.TryRemove(renamedPath, out _);
                            });
                            LogService.Debug($"ImageFolderWatcher: renamed '{Path.GetFileName(filePath)}' to '{Path.GetFileName(renamedPath)}'");
                            filePath = renamedPath;
                        }
                        else
                        {
                            LogService.Warning($"ImageFolderWatcher: failed to rename '{filePath}' to '{renamedPath}' after retries — PendingRenameTarget kept for next file");
                        }
                    }
                }
                else
                {
                    LogService.Debug($"ImageFolderWatcher: no PendingRenameTarget set for '{Path.GetFileName(filePath)}' — skipping rename");
                }

                if (!wasRenamed)
                {
                    _recentlyProcessed.TryRemove(filePath, out _);
                    LogService.Debug($"ImageFolderWatcher: skipping conversion for '{Path.GetFileName(filePath)}' — file was not renamed to a game name");
                    return;
                }

                if (!Path.GetExtension(filePath).Equals(".png", StringComparison.OrdinalIgnoreCase))
                {
                    var (convertedPath, convertError) = await ConvertToPngWithRetryAsync(filePath);
                    if (convertedPath == null)
                    {
                        LogService.Debug($"ImageFolderWatcher: conversion to PNG failed for '{filePath}' — ImageFound NOT fired");
                        ConversionFailed?.Invoke(filePath, convertError ?? "Unknown error");
                        return;
                    }

                    _recentlyProcessed.TryAdd(convertedPath, 1);
                    _ = Task.Run(async () =>
                    {
                        try { await Task.Delay(60000, _disposeCts.Token); } catch (OperationCanceledException) { }
                        _recentlyProcessed.TryRemove(convertedPath, out _);
                    });

                    filePath = convertedPath;
                }

                fileNameWithoutExt = Path.GetFileNameWithoutExtension(filePath);
                LogService.Debug($"ImageFolderWatcher: firing ImageFound for '{fileNameWithoutExt}'");
                ImageFound?.Invoke(fileNameWithoutExt);
            }
            catch (Exception ex)
            {
                LogService.Error(ex, $"Error processing new image in folder: {filePath}");
            }
            finally
            {
                _processingLock.Release();
            }
        }
        catch (Exception ex)
        {
            LogService.Error(ex, "Error processing new image in folder.");
        }
    }

    private static async Task WaitForFileReadyAsync(string filePath)
    {
        const int maxWaitMs = 10000;
        const int pollIntervalMs = 250;
        const int stableChecksRequired = 2;
        var elapsed = 0;
        long lastSize = -1;
        var stableCount = 0;

        while (elapsed < maxWaitMs)
        {
            try
            {
                await using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                if (stream.Length > 0)
                {
                    if (stream.Length == lastSize)
                    {
                        stableCount++;
                        if (stableCount >= stableChecksRequired)
                            return;
                    }
                    else
                    {
                        stableCount = 0;
                        lastSize = stream.Length;
                    }
                }
                else
                {
                    stableCount = 0;
                    lastSize = 0;
                }
            }
            catch (IOException)
            {
                stableCount = 0;
                lastSize = -1;
            }
            catch (UnauthorizedAccessException)
            {
                return;
            }

            await Task.Delay(pollIntervalMs);
            elapsed += pollIntervalMs;
        }
    }

    private static async Task<bool> MoveFileWithRetryAsync(string sourcePath, string targetPath)
    {
        const int maxRetries = 5;
        const int baseDelayMs = 200;

        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                File.Move(sourcePath, targetPath);
                return true;
            }
            catch (FileNotFoundException)
            {
                LogService.Debug($"MoveFileWithRetryAsync: source file not found (may have been deleted): '{sourcePath}'");
                return false;
            }
            catch (IOException) when (attempt < maxRetries)
            {
                var delay = baseDelayMs * Math.Pow(2, attempt - 1);
                await Task.Delay((int)delay);
            }
            catch (UnauthorizedAccessException) when (attempt < maxRetries)
            {
                var delay = baseDelayMs * Math.Pow(2, attempt - 1);
                await Task.Delay((int)delay);
            }
            catch (Exception ex)
            {
                LogService.Error(ex, $"MoveFileWithRetryAsync: attempt {attempt} failed for '{sourcePath}' -> '{targetPath}'");
                if (attempt >= maxRetries) return false;

                var delay = baseDelayMs * Math.Pow(2, attempt - 1);
                await Task.Delay((int)delay);
            }
        }

        return false;
    }

    private static async Task<(string? Path, string? Error)> ConvertToPngWithRetryAsync(string sourcePath)
    {
        const int maxRetries = 3;
        const int baseDelayMs = 300;
        string? lastError = null;

        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                return await ConvertToPngAsync(sourcePath);
            }
            catch (MagickException ex)
            {
                LogService.Warning(ex, $"ImageFolderWatcher: image conversion skipped for corrupt/misformatted file: {sourcePath}");
                return (null, ex.Message);
            }
            catch (Exception ex)
            {
                lastError = ex.Message;
                LogService.Error(ex, $"Failed to convert image to PNG: {sourcePath}");
                if (attempt < maxRetries)
                {
                    LogService.Debug($"ImageFolderWatcher: convert retry {attempt}/{maxRetries} for '{sourcePath}'");
                    var delay = baseDelayMs * Math.Pow(2, attempt - 1);
                    await Task.Delay((int)delay);
                }
            }
        }

        return (null, lastError);
    }

    private static async Task<(string? Path, string? Error)> ConvertToPngAsync(string sourcePath)
    {
        var directory = Path.GetDirectoryName(sourcePath);
        var fileNameWithoutExt = Path.GetFileNameWithoutExtension(sourcePath);
        var targetPath = Path.Combine(directory ?? ".", fileNameWithoutExt + ".png");

        var settings = ImageProcessor.GetMagickReadSettings(sourcePath);
        using var magickImage = new MagickImage(sourcePath, settings);
        magickImage.AutoOrient();
        magickImage.Quality = 90;
        magickImage.Format = MagickFormat.Png;

        await magickImage.WriteAsync(targetPath);

        if (File.Exists(sourcePath) && !string.Equals(sourcePath, targetPath, StringComparison.OrdinalIgnoreCase))
        {
            try { File.Delete(sourcePath); }
            catch (Exception ex)
            {
                LogService.Warning(ex, $"ImageFolderWatcher: failed to delete source file after PNG conversion: '{sourcePath}'");
            }
        }

        return (targetPath, null);
    }

    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;
        _disposeCts.Cancel();
        _disposeCts.Dispose();

        Stop();
        _processingLock.Dispose();
    }
}
