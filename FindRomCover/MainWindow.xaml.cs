using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Documents;
using System.Windows.Input;
using FindRomCover.Managers;
using FindRomCover.models;
using FindRomCover.Services;
using Microsoft.Win32;
using Application = System.Windows.Application;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MenuItem = System.Windows.Controls.MenuItem;
using MessageBox = System.Windows.MessageBox;
using TextBox = System.Windows.Controls.TextBox;

namespace FindRomCover;

public partial class MainWindow : INotifyPropertyChanged, IDisposable
{
    private CancellationTokenSource? _loadMissingCts;
    private List<MameManager>? _machines;
    private Dictionary<string, string>? _mameLookup;
    private CancellationTokenSource? _findSimilarCts;
    private readonly SemaphoreSlim _findSimilarSemaphore = new(1, 1); // Semaphore to ensure only one search runs at a time

    public event PropertyChangedEventHandler? PropertyChanged;
    public SettingsManager Settings => App.SettingsManager;
    private string _selectedRomFileName = string.Empty;

    public ObservableCollection<ImageData> SimilarImages { get; set; } = [];

    public bool IsCheckingMissing
    {
        get;
        set
        {
            if (field == value) return;

            field = value;
            OnPropertyChanged(nameof(IsCheckingMissing));
        }
    }

    public bool IsFindingSimilar
    {
        get;
        set
        {
            if (field == value) return;

            field = value;
            OnPropertyChanged(nameof(IsFindingSimilar));
        }
    }

    public object DisplayImage { get; } = new();
    public object ImageName { get; } = new();
    public object SimilarityScore { get; } = new();

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;

        // Set folder paths from command-line arguments if provided by App.xaml.cs
        if (!string.IsNullOrEmpty(App.StartupImageFolderPath) && !string.IsNullOrEmpty(App.StartupRomFolderPath))
        {
            TxtImageFolder.Text = App.StartupImageFolderPath;
            TxtRomFolder.Text = App.StartupRomFolderPath;
        }
        else
        {
            TxtImageFolder.Text = "";
            TxtRomFolder.Text = "";
        }

        UpdateThumbnailSizeMenuChecks();
        UpdateSimilarityAlgorithmChecks();
        UpdateSimilarityThresholdChecks();
        UpdateAccentColorChecks();
        UpdateBaseThemeMenuChecks();
        UpdateMameDescriptionCheck();

        // Subscribe to SettingsManager PropertyChanged to update UI for non-binding properties (like menu checks)
        Settings.PropertyChanged += AppSettingsManagerPropertyChanged;

        // Load _machines and _mameLookup
        LoadMameData();

        // Initial UI state update based on folder paths
        UpdateUiStateForFolderPaths();
    }

    private void LoadMameData()
    {
        try
        {
            _machines = MameManager.LoadFromDat();

            // Only recreate the lookup dictionary if we have machines data
            // and it's different from what we already have (prevents unnecessary recreation)
            if (_machines is { Count: > 0 })
            {
                _mameLookup = _machines
                    .GroupBy(static m => m.MachineName, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(static g => g.Key, static g => g.First().Description, StringComparer.OrdinalIgnoreCase);
            }

            // If MAME data loaded successfully, ensure menu item is enabled
            MenuUseMameDescription.IsEnabled = true;
            MenuUseMameDescription.ToolTip = null;
        }
        catch (FileNotFoundException ex)
        {
            const string contextMessage = "The file 'mame.dat' could not be found in the application folder.";
            _ = ErrorLogger.LogAsync(ex, contextMessage);

            MenuUseMameDescription.IsEnabled = false;
            MenuUseMameDescription.ToolTip = "MAME data (mame.dat) could not be found.";
            DisableMameDescriptionSetting();
            MessageBox.Show(contextMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        catch (Exception ex)
        {
            var contextMessage = $"Failed to load MAME data from 'mame.dat': {ex.Message}";
            _ = ErrorLogger.LogAsync(ex, contextMessage);

            MenuUseMameDescription.IsEnabled = false;
            MenuUseMameDescription.ToolTip = "MAME data (mame.dat) could not be loaded or is corrupted.";
            DisableMameDescriptionSetting();
            MessageBox.Show(contextMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static void DisableMameDescriptionSetting()
    {
        if (App.SettingsManager.UseMameDescription)
        {
            App.SettingsManager.UseMameDescription = false;
            App.SettingsManager.SaveSettings();
        }
    }

    private void UpdateBaseThemeMenuChecks()
    {
        LightTheme.IsChecked = App.SettingsManager.BaseTheme == "Light";
        DarkTheme.IsChecked = App.SettingsManager.BaseTheme == "Dark";
    }

    private async void AppSettingsManagerPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        try
        {
            // This handler is now only needed for settings that don't update the UI via direct binding,
            // such as menu item checks or triggering a list refresh.
            switch (e.PropertyName)
            {
                case nameof(SettingsManager.BaseTheme):
                    LightTheme.IsChecked = App.SettingsManager.BaseTheme == "Light";
                    DarkTheme.IsChecked = App.SettingsManager.BaseTheme == "Dark";
                    break;
                case nameof(SettingsManager.AccentColor):
                    UpdateAccentColorChecks();
                    break;
                case nameof(SettingsManager.ImageWidth):
                case nameof(SettingsManager.ImageHeight):
                    // The UI is updated via data binding. We just need to update the menu checks.
                    UpdateThumbnailSizeMenuChecks();
                    break;
                case nameof(SettingsManager.SelectedSimilarityAlgorithm):
                    UpdateSimilarityAlgorithmChecks();
                    break;
                case nameof(SettingsManager.SimilarityThreshold):
                    UpdateSimilarityThresholdChecks();
                    break;
                case nameof(SettingsManager.UseMameDescription):
                    UpdateMameDescriptionCheck();
                    if (_mameLookup is { Count: > 0 })
                        await RefreshMissingImagesList();
                    break;
            }
        }
        catch (Exception ex)
        {
            _ = ErrorLogger.LogAsync(ex, "Error in AppSettings_PropertyChanged");
        }
    }

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void ChangeBaseTheme_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem menuItem) return;

        var theme = menuItem.Name == "LightTheme" ? "Light" : "Dark";
        App.ChangeTheme(theme, App.SettingsManager.AccentColor); // Use static App.ChangeTheme
    }

    private void ChangeAccentColor_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem menuItem) return;

        var accent = menuItem.Name.Replace("Accent", "");
        App.ChangeTheme(App.SettingsManager.BaseTheme, accent); // Use static App.ChangeTheme
    }

    private void UpdateAccentColorChecks()
    {
        var currentAccent = App.SettingsManager.AccentColor;
        foreach (var item in MenuAccentColors.Items)
        {
            if (item is not MenuItem { Header: not null } menuItem) continue;

            menuItem.IsChecked = menuItem.Header.ToString()?.Replace("Accent", "") == currentAccent;
        }
    }

    private void DonateButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "https://www.purelogiccode.com/donate",
                UseShellExecute = true
            };
            System.Diagnostics.Process.Start(psi);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Unable to open the donation link: {ex.Message}",
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);

            var formattedException = $"Unable to open the donation link.\n\n" +
                                     $"Exception type: {ex.GetType().Name}\n" +
                                     $"Exception details: {ex.Message}";
            _ = ErrorLogger.LogAsync(ex, formattedException);
        }
    }

    private void About_Click(object sender, RoutedEventArgs e)
    {
        AboutWindow aboutWindow = new();
        aboutWindow.ShowDialog();
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void BtnBrowseRomFolder_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Select the folder where your ROM or ISO files are stored."
        };

        if (dialog.ShowDialog() == true)
        {
            TxtRomFolder.Text = dialog.FolderName;
            UpdateUiStateForFolderPaths(); // Update UI state after browsing
        }
    }

    private void BtnBrowseImageFolder_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Select the folder where your image files are stored."
        };

        if (dialog.ShowDialog() != true) return;

        TxtImageFolder.Text = dialog.FolderName;
        UpdateUiStateForFolderPaths(); // Update UI state after browsing
    }

    private async void BtnCheckForMissingImages_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await RefreshMissingImagesList();
        }
        catch (Exception ex)
        {
            _ = ErrorLogger.LogAsync(ex, "Error in BtnCheckForMissingImages_Click");
        }
    }

    private async Task LoadMissingImagesList(CancellationToken cancellationToken = default)
    {
        if ((App.SettingsManager.SupportedExtensions.Length == 0))
        {
            MessageBox.Show("No supported file extensions loaded. Please check file 'settings.xml' or edit them in the Settings menu.", "Warning",
                MessageBoxButton.OK, MessageBoxImage.Warning);

            return;
        }

        var romFolderPath = GetValidatedRomFolderPath();
        var imageFolderPath = GetValidatedImageFolderPath();

        if (string.IsNullOrEmpty(romFolderPath) || string.IsNullOrEmpty(imageFolderPath))
        {
            MessageBox.Show("Please select both ROM and Image folders.", "Warning",
                MessageBoxButton.OK, MessageBoxImage.Warning);

            return;
        }

        IsCheckingMissing = true;
        try
        {
            var missingFiles = await Task.Run(async () =>
            {
                try
                {
                    // Check for cancellation at the start
                    cancellationToken.ThrowIfCancellationRequested();

                    var allRomNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    var supportedExtensionsSet = new HashSet<string>(
                        App.SettingsManager.SupportedExtensions.Select(static ext => "." + ext),
                        StringComparer.OrdinalIgnoreCase);

                    var enumerationOptions = new EnumerationOptions
                    {
                        IgnoreInaccessible = true, // Skip directories that can't be accessed.
                        RecurseSubdirectories = true
                    };

                    // Scan the directory tree once for all files, ignoring inaccessible subdirectories.
                    var files = Directory.EnumerateFiles(romFolderPath, "*.*", enumerationOptions);

                    foreach (var file in files)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        // Filter by extension in memory.
                        var extension = Path.GetExtension(file);
                        if (supportedExtensionsSet.Contains(extension))
                        {
                            var fileName = Path.GetFileNameWithoutExtension(file);
                            if (!string.IsNullOrEmpty(fileName))
                                allRomNames.Add(fileName);
                        }
                    }

                    var missing = new List<(string RomName, string SearchName)>();

                    // Process ROMs with periodic cancellation checks
                    var processedCount = 0;
                    foreach (var romName in allRomNames)
                    {
                        // Check for cancellation periodically (every 100 items)
                        if (++processedCount % 100 == 0)
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            await Task.Yield(); // Yield periodically to prevent blocking
                        }

                        if (FindCorrespondingImage(romName, imageFolderPath) == null)
                        {
                            if (App.SettingsManager.UseMameDescription &&
                                _mameLookup != null &&
                                _mameLookup.TryGetValue(romName, out var description) &&
                                !string.IsNullOrEmpty(description))
                            {
                                missing.Add((romName, description));
                            }
                            else
                            {
                                missing.Add((romName, romName));
                            }
                        }
                    }

                    // Final cancellation check before returning
                    cancellationToken.ThrowIfCancellationRequested();

                    return missing.OrderBy(static x => x.RomName).ToList();
                }
                catch (OperationCanceledException)
                {
                    // Re-throw cancellation exceptions
                    throw;
                }
                catch (UnauthorizedAccessException ex)
                {
                    throw new InvalidOperationException("Access denied to folder. Try running as administrator.", ex);
                }
                catch (DirectoryNotFoundException ex)
                {
                    throw new InvalidOperationException("Folder not found. Please check the paths.", ex);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Error scanning folders: {ex.Message}", ex);
                }
            }, cancellationToken);

            // Check cancellation before updating UI
            cancellationToken.ThrowIfCancellationRequested();

            // Only update UI if we got valid results
            LstMissingImages.Items.Clear();
            SimilarImages.Clear();

            foreach (var (romName, searchName) in missingFiles)
            {
                LstMissingImages.Items.Add(new { RomName = romName, SearchName = searchName });
            }

            UpdateMissingCount();
        }
        catch (OperationCanceledException)
        {
            // Operation was cancelled - clean up UI state but don't show error
        }
        catch (Exception ex)
        {
            // Show user-friendly error
            string userMessage;
            if (ex is InvalidOperationException ioEx)
            {
                userMessage = ioEx.Message;
            }
            else
            {
                userMessage =
                    "There was an error checking for missing image files.\n\nCheck if the provided folders are valid.";
            }

            MessageBox.Show(userMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error);

            // Log detailed error
            var formattedException = $"Error checking for missing images: {ex.Message}";
            _ = ErrorLogger.LogAsync(ex, formattedException);
        }
        finally
        {
            IsCheckingMissing = false;
        }
    }

    private static string? FindCorrespondingImage(string fileNameWithoutExtension, string imageFolderPath)
    {
        string[] imageExtensions = [".png", ".jpg", ".jpeg"];
        foreach (var ext in imageExtensions)
        {
            var imagePath = Path.Combine(imageFolderPath, fileNameWithoutExtension + ext);
            if (File.Exists(imagePath))
            {
                return imagePath;
            }
        }

        return null;
    }

    private async void LstMissingImages_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        try
        {
            // --- Start of method: setup and early exit conditions ---
            _findSimilarCts?.Cancel();
            _findSimilarCts?.Dispose();

            if (LstMissingImages.SelectedItem == null)
            {
                SimilarImages.Clear();
                LblSearchQuery.Content = null;
                IsFindingSimilar = false;
                _findSimilarCts = null;
                return;
            }

            var imageFolderPath = GetValidatedImageFolderPath();
            if (string.IsNullOrEmpty(imageFolderPath))
            {
                IsFindingSimilar = false;
                _findSimilarCts = null;
                return;
            }

            _findSimilarCts = new CancellationTokenSource();
            var cancellationToken = _findSimilarCts.Token;

            dynamic selectedItem = LstMissingImages.SelectedItem;
            string romName = selectedItem.RomName;
            string searchName = selectedItem.SearchName;
            _selectedRomFileName = romName;

            // --- Core logic with robust progress indicator handling ---
            IsFindingSimilar = true;
            try
            {
                await _findSimilarSemaphore.WaitAsync(cancellationToken);
                try
                {
                    var similarityResult = await ButtonFactory.CreateSimilarImagesCollection(
                        searchName,
                        imageFolderPath,
                        Settings.SimilarityThreshold,
                        Settings.SelectedSimilarityAlgorithm,
                        cancellationToken
                    );

                    // Only update UI if operation wasn't cancelled
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            var textBlock = new TextBlock();
                            textBlock.Inlines.Add(new Run("Search Query: "));
                            textBlock.Inlines.Add(new Run($"{searchName} ") { FontWeight = FontWeights.Bold });
                            textBlock.Inlines.Add(new Run("for ROM: "));
                            textBlock.Inlines.Add(new Run($"{romName} ") { FontWeight = FontWeights.Bold });
                            textBlock.Inlines.Add(new Run("with "));
                            textBlock.Inlines.Add(new Run($"{Settings.SelectedSimilarityAlgorithm} ") { FontWeight = FontWeights.Bold });
                            textBlock.Inlines.Add(new Run("algorithm"));
                            LblSearchQuery.Content = textBlock;

                            SimilarImages.Clear();
                            foreach (var imageData in similarityResult.SimilarImages) // Use SimilarImages from result
                            {
                                SimilarImages.Add(imageData);
                            }

                            // --- NEW: Display processing errors to the user ---
                            if (similarityResult.ProcessingErrors.Count > 0)
                            {
                                var errorSummary = $"Encountered {similarityResult.ProcessingErrors.Count} issues while processing images:\n\n";
                                errorSummary += string.Join("\n", similarityResult.ProcessingErrors.Take(5)); // Show first 5 errors
                                if (similarityResult.ProcessingErrors.Count > 5)
                                {
                                    errorSummary += $"\n...and {similarityResult.ProcessingErrors.Count - 5} more. Check the application log for full details.";
                                }

                                MessageBox.Show(errorSummary, "Image Processing Warnings", MessageBoxButton.OK, MessageBoxImage.Warning);
                            }
                            // --- END NEW ---
                        });

                        ImageScrollViewer.ScrollToTop();
                    }
                }
                finally
                {
                    _findSimilarSemaphore.Release();
                }
            }
            catch (OperationCanceledException)
            {
                // This is an expected outcome when the selection changes quickly.
                // No action or logging needed. The 'finally' block ensures cleanup.
            }
            catch (Exception ex)
            {
                // For any other unexpected error, log it and notify the user.
                // The check for cancellationToken prevents showing an error for a cancelled operation
                // that might throw a different exception type during unwinding.
                if (!cancellationToken.IsCancellationRequested)
                {
                    MessageBox.Show($"An error occurred while searching for similar images: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    _ = ErrorLogger.LogAsync(ex, "Error in LstMissingImages_SelectionChanged");
                }
            }
            finally
            {
                // This block ensures the progress ring is always turned off,
                // whether the operation succeeded, was cancelled, or failed.
                IsFindingSimilar = false;
            }
        }
        catch (Exception ex)
        {
            _ = ErrorLogger.LogAsync(ex, "Error in LstMissingImages_SelectionChanged");
        }
    }

    private void ImageCell_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: ImageData { ImagePath: not null } imageData })
        {
            UseImage(imageData.ImagePath);
        }
    }

    public void UseImage(string? imagePath)
    {
        var imageFolderPath = GetValidatedImageFolderPath();

        if (string.IsNullOrEmpty(_selectedRomFileName) ||
            string.IsNullOrEmpty(imagePath) ||
            string.IsNullOrEmpty(imageFolderPath))
        {
            return;
        }

        var newFileName = Path.Combine(imageFolderPath, _selectedRomFileName + ".png");

        try
        {
            if (ImageProcessor.ConvertAndSaveImage(imagePath, newFileName))
            {
                App.AudioService.PlayClickSound();
                RemoveSelectedItem();
                SimilarImages.Clear();
                UpdateMissingCount();
            }
            else
            {
                MessageBox.Show("Failed to save the image.", "Save Failed",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Unexpected error saving image: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
            _ = ErrorLogger.LogAsync(ex, $"Unexpected error in UseImage: {imagePath}");
        }
    }

    private void RemoveSelectedItem()
    {
        if (LstMissingImages.SelectedItem == null || LstMissingImages.SelectedIndex < 0)
            return;

        try
        {
            var oldIndex = LstMissingImages.SelectedIndex;
            LstMissingImages.Items.RemoveAt(oldIndex);

            // Select next logical item
            if (LstMissingImages.Items.Count > 0)
            {
                // Calculate new index - ensure it's valid
                var newIndex = Math.Min(oldIndex, LstMissingImages.Items.Count - 1);

                LstMissingImages.SelectedIndex = newIndex;

                // Only scroll if we have a valid item to scroll to
                if (newIndex >= 0 && LstMissingImages.Items.Count > newIndex)
                {
                    var itemToScrollTo = LstMissingImages.Items[newIndex];
                    if (itemToScrollTo != null) LstMissingImages.ScrollIntoView(itemToScrollTo);
                }
            }
            else
            {
                // If no items remain, explicitly clear the search query label
                LblSearchQuery.Content = null;
            }
        }
        catch (Exception ex)
        {
            _ = ErrorLogger.LogAsync(ex, "Error in RemoveSelectedItem");
        }

        UpdateMissingCount();
    }

    private void UpdateMissingCount()
    {
        LabelMissingRoms.Content = "MISSING COVERS: " + LstMissingImages.Items.Count;
    }

    private void SetSimilarityAlgorithm_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem menuItem) return;

        var algorithm = menuItem.Header.ToString() ?? "Jaro-Winkler Distance";
        App.SettingsManager.SelectedSimilarityAlgorithm = algorithm; // Update App.Settings
        App.SettingsManager.SaveSettings(); // Save the settings
    }

    private void UpdateSimilarityAlgorithmChecks()
    {
        foreach (var item in MenuSimilarityAlgorithms.Items)
        {
            if (item is MenuItem menuItem)
            {
                menuItem.IsChecked = menuItem.Header.ToString() == App.SettingsManager.SelectedSimilarityAlgorithm; // Use App.Settings
            }
        }
    }

    private void SetSimilarityThreshold_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem clickedItem)
        {
            return;
        }

        try
        {
            var headerText = clickedItem.Header.ToString()?.Replace("%", "") ?? "70";

            if (double.TryParse(headerText, out var rate))
            {
                App.SettingsManager.SimilarityThreshold = rate;
                App.SettingsManager.SaveSettings();
            }
            else
            {
                MessageBox.Show("Invalid similarity threshold selected.\n\n" +
                                "The error was reported to the developer that will try to fix the issue.",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                const string formattedException = "Invalid similarity threshold selected.";
                var ex = new ArgumentException(formattedException);
                _ = ErrorLogger.LogAsync(ex, formattedException);
            }
        }
        catch (Exception ex)
        {
            _ = ErrorLogger.LogAsync(ex, "Error in SetSimilarityThreshold_Click");
        }
    }

    private void UpdateSimilarityThresholdChecks()
    {
        var currentThreshold = App.SettingsManager.SimilarityThreshold;

        foreach (var item in MySimilarityMenu.Items)
        {
            if (item is not MenuItem menuItem) continue;

            var thresholdString = menuItem.Header.ToString()?.Replace("%", "") ?? "70";

            if (double.TryParse(thresholdString, NumberStyles.Any, CultureInfo.InvariantCulture, out var menuItemThreshold))
            {
                // Use a small epsilon for floating-point comparison
                const double epsilon = 0.001;
                menuItem.IsChecked = Math.Abs(menuItemThreshold - currentThreshold) < epsilon;
            }
        }
    }

    private void SetThumbnailSize_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem menuItem)
        {
            return;
        }

        // Get size from Tag property instead of parsing header text
        if (menuItem.Tag is not int size && !int.TryParse(menuItem.Tag?.ToString(), out size))
        {
            MessageBox.Show("Invalid thumbnail size selected.",
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        try
        {
            App.SettingsManager.ImageWidth = size;
            App.SettingsManager.ImageHeight = size;
            App.SettingsManager.SaveSettings();
        }
        catch (Exception ex)
        {
            _ = ErrorLogger.LogAsync(ex, "Error in SetThumbnailSize_Click");
        }
    }

    private void UpdateThumbnailSizeMenuChecks()
    {
        var currentSize = App.SettingsManager.ImageWidth;

        foreach (var item in ImageSizeMenu.Items)
        {
            if (item is not MenuItem menuItem) continue;

            // Get size from Tag property instead of parsing header text
            if (menuItem.Tag is int size || int.TryParse(menuItem.Tag?.ToString(), out size))
            {
                menuItem.IsChecked = size == currentSize;
            }
        }
    }

    private void Image_ContextMenuOpening(object sender, ContextMenuEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: ImageData imageData } element) return;

        if (imageData.ImagePath != null)
        {
            // Pass the UseImage method as the action and reuse cached menu if available
            element.ContextMenu = ButtonFactory.CreateContextMenu(imageData.ImagePath, UseImage, imageData.CachedContextMenu);
            // Store the menu in the ImageData for future reuse
            imageData.CachedContextMenu = element.ContextMenu;
        }
    }

    private void EditExtensions_Click(object sender, RoutedEventArgs e)
    {
        var settingsWindow = new SettingsWindow(App.SettingsManager) // Pass App.Settings
        {
            Owner = this
        };
        settingsWindow.ShowDialog();
    }

    private void TxtImageFolder_TextChanged(object sender, TextChangedEventArgs e)
    {
        // Only update button enabled state based on non-empty text,
        // full Directory.Exists validation happens on LostFocus/Enter.
        BtnCheckForMissingImages.IsEnabled = !string.IsNullOrEmpty(TxtRomFolder.Text.Trim()) && !string.IsNullOrEmpty(TxtImageFolder.Text.Trim());
        LstMissingImages.IsEnabled = BtnCheckForMissingImages.IsEnabled; // Keep LstMissingImages enabled state in sync
        if (!BtnCheckForMissingImages.IsEnabled)
        {
            LstMissingImages.Items.Clear();
            SimilarImages.Clear();
            LblSearchQuery.Content = null;
            UpdateMissingCount();
        }
    }

    private void LstMissingImages_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Delete)
        {
            RemoveSelectedItem();
            App.AudioService.PlayClickSound();
        }
    }

    private async void MenuUseMameDescription_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (sender is MenuItem menuItem)
            {
                // Handle nullable bool properly
                var isChecked = menuItem.IsChecked;
                App.SettingsManager.UseMameDescription = isChecked;
                App.SettingsManager.SaveSettings();

                // Refresh the list immediately after the setting changes
                // Only refresh if MAME data is actually available, otherwise it's pointless
                if (_mameLookup is { Count: > 0 })
                    await RefreshMissingImagesList();
            }
        }
        catch (Exception ex)
        {
            _ = ErrorLogger.LogAsync(ex, "Error in MenuUseMameDescription_Click");
        }
    }

    private async Task RefreshMissingImagesList()
    {
        // Cancel any existing operation
        if (_loadMissingCts != null)
        {
            await _loadMissingCts.CancelAsync();
            _loadMissingCts.Dispose();
            _loadMissingCts = null;
        }

        _loadMissingCts = new CancellationTokenSource();
        try
        {
            await LoadMissingImagesList(_loadMissingCts.Token);
        }
        catch (OperationCanceledException)
        {
            // Expected if a previous operation was cancelled or if this one was cancelled quickly.
            // No need to log this as an error.
        }
        catch (Exception ex)
        {
            _ = ErrorLogger.LogAsync(ex, "Error refreshing missing images list.");
        }
        finally
        {
            // Ensure CTS is disposed even if an exception occurs during LoadMissingImagesList
            _loadMissingCts?.Dispose();
            _loadMissingCts = null;
        }
    }

    private void TxtImageFolder_LostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            var newPath = textBox.Text.Trim();

            // Only update the 'last valid path' if the new path is actually valid.
            // Do not revert the text if it's invalid, allowing the user to correct it.
            if (!string.IsNullOrEmpty(newPath) && Directory.Exists(newPath))
            {
            }

            UpdateUiStateForFolderPaths();
        }
    }

    private void UpdateMameDescriptionCheck()
    {
        MenuUseMameDescription.IsChecked = App.SettingsManager.UseMameDescription;
    }

    private void BtnRemoveSelectedItem_Click(object sender, RoutedEventArgs e)
    {
        RemoveSelectedItem();
        SimilarImages.Clear();
        App.AudioService.PlayClickSound();
    }

    private void TxtImageFolder_PreviewKeyDown(object sender, KeyEventArgs keyEventArgs)
    {
        if (keyEventArgs.Key == Key.Enter && sender is TextBox) // Corrected line
        {
            // Trigger the same logic as LostFocus when Enter is pressed
            TxtImageFolder_LostFocus(sender, new RoutedEventArgs(LostFocusEvent, sender));
            // Prevent the Enter key from being processed further (e.g., adding a newline)
            keyEventArgs.Handled = true; // Corrected line
        }
    }

    private void TxtRomFolder_LostFocus(object sender, RoutedEventArgs routedEventArgs)
    {
        if (sender is TextBox textBox)
        {
            var newPath = textBox.Text.Trim();

            // Only update the 'last valid path' if the new path is actually valid.
            // Do not revert the text if it's invalid, allowing the user to correct it.
            if (!string.IsNullOrEmpty(newPath) && Directory.Exists(newPath))
            {
            }

            UpdateUiStateForFolderPaths();
        }
    }

    private void TxtRomFolder_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && sender is TextBox)
        {
            TxtRomFolder_LostFocus(sender, e);
            e.Handled = true;
        }
    }

    private void TxtRomFolder_TextChanged(object sender, TextChangedEventArgs e)
    {
        // Only update button enabled state based on non-empty text,
        // full Directory.Exists validation happens on LostFocus/Enter.
        BtnCheckForMissingImages.IsEnabled = !string.IsNullOrEmpty(TxtRomFolder.Text.Trim()) && !string.IsNullOrEmpty(TxtImageFolder.Text.Trim());
        LstMissingImages.IsEnabled = BtnCheckForMissingImages.IsEnabled; // Keep LstMissingImages enabled state in sync
        if (!BtnCheckForMissingImages.IsEnabled)
        {
            LstMissingImages.Items.Clear();
            SimilarImages.Clear();
            LblSearchQuery.Content = null;
            UpdateMissingCount();
        }
    }

    private string? GetValidatedImageFolderPath(bool showWarning = true)
    {
        return ValidateFolderPath(TxtImageFolder.Text.Trim(), "Image", showWarning);
    }

    private string? GetValidatedRomFolderPath(bool showWarning = true)
    {
        return ValidateFolderPath(TxtRomFolder.Text.Trim(), "ROM", showWarning);
    }

    private static string? ValidateFolderPath(string path, string folderType, bool showWarning)
    {
        if (string.IsNullOrEmpty(path))
        {
            return null;
        }

        if (Directory.Exists(path))
        {
            return path;
        }

        if (showWarning)
        {
            MessageBox.Show($"The {folderType.ToLowerInvariant()} folder path '{path}' is invalid or does not exist. Please correct it.",
                $"Invalid {folderType} Folder", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        return null;
    }

    private void UpdateUiStateForFolderPaths()
    {
        var romPath = TxtRomFolder.Text.Trim();
        var imagePath = TxtImageFolder.Text.Trim();

        var romPathValid = !string.IsNullOrEmpty(romPath) && Directory.Exists(romPath);
        var imagePathValid = !string.IsNullOrEmpty(imagePath) && Directory.Exists(imagePath);

        BtnCheckForMissingImages.IsEnabled = romPathValid && imagePathValid;
        LstMissingImages.IsEnabled = romPathValid && imagePathValid;

        // Also clear suggestions if paths become invalid
        if (!romPathValid || !imagePathValid)
        {
            LstMissingImages.Items.Clear();
            SimilarImages.Clear();
            LblSearchQuery.Content = null;
            UpdateMissingCount();
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        Settings.PropertyChanged -= AppSettingsManagerPropertyChanged;
        Dispose();
        base.OnClosed(e);
    }

    public void Dispose()
    {
        // Cancel any pending operations first
        _findSimilarCts?.Cancel();
        _loadMissingCts?.Cancel();

        // Now dispose resources
        _findSimilarCts?.Dispose();
        _loadMissingCts?.Dispose();
        _findSimilarSemaphore.Dispose();

        GC.SuppressFinalize(this);
    }
}