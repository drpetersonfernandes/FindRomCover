using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.ComponentModel;
using System.Globalization;
using System.Text.RegularExpressions;
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
    private string? _lastValidRomFolderPath;
    private string? _lastValidImageFolderPath;

    private CancellationTokenSource? _loadMissingCts;

    private List<MameManager>? _machines;
    private Dictionary<string, string>? _mameLookup;

    private Task? _currentFindTask; // Task for the current image similarity search
    private CancellationTokenSource? _findSimilarCts; // CancellationTokenSource for the current image similarity search
    private readonly SemaphoreSlim _findSimilarSemaphore = new(1, 1); // Semaphore to ensure only one search runs at a time

    public event PropertyChangedEventHandler? PropertyChanged;
    private string _selectedRomFileName = string.Empty;

    public ObservableCollection<ImageData> SimilarImages { get; set; } = [];
    private const string DefaultSimilarityAlgorithm = "Jaro-Winkler Distance";

    private int _imageWidth;

    public int ImageWidth
    {
        get => _imageWidth;
        set
        {
            if (_imageWidth == value) return;

            _imageWidth = value;
            OnPropertyChanged(nameof(ImageWidth));
            // Removed direct App.Settings update here. Handled by SetThumbnailSize_Click.
        }
    }

    private int _imageHeight;

    public int ImageHeight
    {
        get => _imageHeight;
        set
        {
            if (_imageHeight == value) return;

            _imageHeight = value;
            OnPropertyChanged(nameof(ImageHeight));
        }
    }

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

    private string _selectedSimilarityAlgorithm; // Backing field for property

    public string SelectedSimilarityAlgorithm
    {
        get => _selectedSimilarityAlgorithm;
        set
        {
            if (_selectedSimilarityAlgorithm == value) return;

            _selectedSimilarityAlgorithm = value;
            OnPropertyChanged(nameof(SelectedSimilarityAlgorithm));
            // Removed direct App.Settings update here. Handled by SetSimilarityAlgorithm_Click.
        }
    }

    public object DisplayImage { get; } = new();
    public object ImageName { get; } = new();
    public object SimilarityScore { get; } = new();

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;

        // Initialize local properties from App.Settings (direct backing field assignment to avoid PropertyChanged during init)
        _imageWidth = App.SettingsManager.ImageWidth;
        _imageHeight = App.SettingsManager.ImageHeight;
        _selectedSimilarityAlgorithm = App.SettingsManager.SelectedSimilarityAlgorithm;

        // Initialize stored folder paths
        _lastValidRomFolderPath = "";
        _lastValidImageFolderPath = "";

        // Check for command-line arguments
        var args = Environment.GetCommandLineArgs();
        if (args.Length == 3)
        {
            var imageFolderPath = args[1];
            var romFolderPath = args[2];

            if (Directory.Exists(imageFolderPath) && Directory.Exists(romFolderPath))
            {
                TxtImageFolder.Text = imageFolderPath;
                TxtRomFolder.Text = romFolderPath;
            }
            else
            {
                var invalidPaths = new List<string>();
                if (!Directory.Exists(imageFolderPath))
                    invalidPaths.Add($"Image folder: '{imageFolderPath}'");
                if (!Directory.Exists(romFolderPath))
                    invalidPaths.Add($"ROM folder: '{romFolderPath}'");

                MessageBox.Show(
                    $"The following command-line paths are invalid or do not exist:\n\n{string.Join("\n", invalidPaths)}\n\nThe application will start with empty folder paths.",
                    "Invalid Command-Line Arguments", MessageBoxButton.OK, MessageBoxImage.Warning);

                TxtImageFolder.Text = "";
                TxtRomFolder.Text = "";
            }
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

        // Subscribe to App.Settings PropertyChanged to update UI if settings change elsewhere (e.g. SettingsWindow)
        App.SettingsManager.PropertyChanged += AppSettingsManagerPropertyChanged;

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
            _mameLookup = _machines
                .GroupBy(static m => m.MachineName, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(static g => g.Key, static g => g.First().Description, StringComparer.OrdinalIgnoreCase);

            // If MAME data loaded successfully, ensure menu item is enabled
            MenuUseMameDescription.IsEnabled = true;
            MenuUseMameDescription.ToolTip = null;
        }
        catch
        {
            // Notify developer
            const string contextMessage = "The file 'mame.dat' could not be found in the application folder.";
            _ = LogErrors.LogErrorAsync(null, contextMessage);

            // Disable MAME description option if data is not available
            MenuUseMameDescription.IsEnabled = false;
            MenuUseMameDescription.ToolTip = "MAME data (mame.dat) could not be loaded or is corrupted.";
            if (App.SettingsManager.UseMameDescription) // If it was previously enabled, turn it off
            {
                App.SettingsManager.UseMameDescription = false;
                App.SettingsManager.SaveSettings();
            }

            MessageBox.Show(contextMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
            switch (e.PropertyName)
            {
                case nameof(SettingsManager.BaseTheme):
                    // Base theme is handled by App.ChangeTheme, which applies globally.
                    // Ensure menu checks are updated.
                    LightTheme.IsChecked = App.SettingsManager.BaseTheme == "Light";
                    DarkTheme.IsChecked = App.SettingsManager.BaseTheme == "Dark";
                    break;
                case nameof(SettingsManager.AccentColor):
                    // Accent color is handled by App.ChangeTheme, which applies globally.
                    // Ensure menu checks are updated.
                    UpdateAccentColorChecks();
                    break;
                case nameof(SettingsManager.ImageWidth):
                case nameof(SettingsManager.ImageHeight):
                    // Update local properties and trigger UI update
                    _imageWidth = App.SettingsManager.ImageWidth;
                    _imageHeight = App.SettingsManager.ImageHeight;
                    OnPropertyChanged(nameof(ImageWidth));
                    OnPropertyChanged(nameof(ImageHeight));
                    UpdateThumbnailSizeMenuChecks();
                    break;
                case nameof(SettingsManager.SelectedSimilarityAlgorithm):
                    _selectedSimilarityAlgorithm = App.SettingsManager.SelectedSimilarityAlgorithm;
                    OnPropertyChanged(nameof(SelectedSimilarityAlgorithm));
                    UpdateSimilarityAlgorithmChecks();
                    break;
                case nameof(SettingsManager.SimilarityThreshold):
                    UpdateSimilarityThresholdChecks();
                    break;
                case nameof(SettingsManager.UseMameDescription):
                    UpdateMameDescriptionCheck();
                    // Refresh the list immediately after the setting changes
                    // Only refresh if MAME data is actually available, otherwise it's pointless
                    if (_mameLookup != null && _mameLookup.Count > 0)
                        await RefreshMissingImagesList();
                    break;
            }
        }
        catch (Exception ex)
        {
            _ = LogErrors.LogErrorAsync(ex, "Error in AppSettings_PropertyChanged");
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
            _ = LogErrors.LogErrorAsync(ex, formattedException);
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
            _ = LogErrors.LogErrorAsync(ex, "Error in BtnCheckForMissingImages_Click");
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
            var missingFiles = await Task.Run(() =>
            {
                try
                {
                    // Check for cancellation at the start
                    cancellationToken.ThrowIfCancellationRequested();

                    var searchPatterns = App.SettingsManager.SupportedExtensions.Select(static ext => "*." + ext).ToArray();
                    var allRomNames = new List<string>();

                    // Process each pattern with cancellation checks
                    foreach (var pattern in searchPatterns)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        try
                        {
                            var files = Directory.EnumerateFiles(romFolderPath, pattern, SearchOption.AllDirectories);
                            var names = files.Select(Path.GetFileNameWithoutExtension)
                                .Where(static name => name != null)
                                .Cast<string>();
                            allRomNames.AddRange(names);
                        }
                        catch (DirectoryNotFoundException)
                        {
                            // Skip this pattern if directory issues occur
                            continue;
                        }
                    }

                    // Remove duplicates
                    allRomNames = allRomNames.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

                    var missing = new List<(string RomName, string SearchName)>();
                    var processedCount = 0;

                    foreach (var romName in allRomNames)
                    {
                        // Check for cancellation periodically (every 100 items)
                        if (++processedCount % 100 == 0)
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                        }

                        if (romName != null && FindCorrespondingImage(romName, imageFolderPath) == null)
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

                    return missing.OrderBy(x => x.RomName).ToList();
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
            return;
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
            _ = LogErrors.LogErrorAsync(ex, formattedException);
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
            // Acquire semaphore before starting any async work
            await _findSimilarSemaphore.WaitAsync();

            try
            {
                // Cancel previous operation
                if (_findSimilarCts != null)
                {
                    await _findSimilarCts.CancelAsync();
                    try
                    {
                        // Wait for the previous task to complete
                        if (_currentFindTask != null) await _currentFindTask;
                    }
                    catch (OperationCanceledException)
                    {
                        // Expected — ignore
                    }
                    catch (Exception ex)
                    {
                        // Log unexpected errors from previous task
                        _ = LogErrors.LogErrorAsync(ex, "Error in previous find operation");
                    }
                    finally
                    {
                        _findSimilarCts?.Dispose();
                        _findSimilarCts = null;
                        _currentFindTask = null; // Ensure task reference is cleared
                    }
                }

                // Clear results if no selection
                if (LstMissingImages.SelectedItem == null)
                {
                    SimilarImages.Clear();
                    LblSearchQuery.Content = null;
                    IsFindingSimilar = false;
                    return;
                }

                // --- FIX: Get the image folder path correctly ---
                // Use the validated path getter. If it's invalid, show a message and abort.
                var imageFolderPath = GetValidatedImageFolderPath();
                if (string.IsNullOrEmpty(imageFolderPath))
                {
                    // GetValidatedImageFolderPath already shows a MessageBox on failure if showWarning is true (default).
                    // If we reach here, it means the path was invalid or the user was warned.
                    IsFindingSimilar = false; // Ensure the loading indicator turns off
                    return; // Abort the search
                }
                // --- END FIX ---

                // Prepare for new operation
                _findSimilarCts = new CancellationTokenSource();
                var cancellationToken = _findSimilarCts.Token;

                dynamic selectedItem = LstMissingImages.SelectedItem;
                string romName = selectedItem.RomName;
                string searchName = selectedItem.SearchName;

                IsFindingSimilar = true;

                // Create the task and store it
                // --- FIX: Pass the correctly obtained imageFolderPath ---
                var findTask = ButtonFactory.CreateSimilarImagesCollection(
                    searchName,
                    imageFolderPath, // Use the validated path obtained above
                    App.SettingsManager.SimilarityThreshold,
                    SelectedSimilarityAlgorithm,
                    cancellationToken
                );
                // --- END FIX ---
                _currentFindTask = findTask; // This is now Task<SimilarityCalculationResult>

                try
                {
                    _selectedRomFileName = romName;

                    // Await the task to get the result
                    var similarityResult = await findTask; // Get the new result object

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
                            textBlock.Inlines.Add(new Run($"{SelectedSimilarityAlgorithm} ") { FontWeight = FontWeights.Bold });
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

                        if (!cancellationToken.IsCancellationRequested)
                        {
                            ImageScrollViewer.ScrollToTop();
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // MessageBox.Show("Search cancelled.", "Cancelled", MessageBoxButton.OK, MessageBoxImage.Information);
                    // Ignore
                }
                catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
                {
                    MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    _ = LogErrors.LogErrorAsync(ex, "Error in LstMissingImages_SelectionChanged");
                }
                finally
                {
                    IsFindingSimilar = false; // Always reset UI state
                    _currentFindTask = null; // Clear the task reference when done
                }
            }
            catch (Exception ex)
            {
                _ = LogErrors.LogErrorAsync(ex, "Error in LstMissingImages_SelectionChanged outer catch");
                IsFindingSimilar = false;
                _currentFindTask = null;
            }
            finally
            {
                _findSimilarSemaphore.Release(); // Release semaphore
            }
        }
        catch (Exception ex)
        {
            _ = LogErrors.LogErrorAsync(ex, "Error in LstMissingImages_SelectionChanged outer catch");
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
        var imageFolderPath = GetValidatedImageFolderPath(); // Get validated path

        if (string.IsNullOrEmpty(_selectedRomFileName) ||
            string.IsNullOrEmpty(imagePath) ||
            string.IsNullOrEmpty(imageFolderPath)) // Use the validated path
        {
            return;
        }

        var newFileName = Path.Combine(imageFolderPath, _selectedRomFileName + ".png"); // Use the validated path

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
            _ = LogErrors.LogErrorAsync(ex, $"Unexpected error in UseImage: {imagePath}");
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
            _ = LogErrors.LogErrorAsync(ex, "Error in RemoveSelectedItem");
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

        var algorithm = menuItem.Header.ToString() ?? DefaultSimilarityAlgorithm;
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
                var ex = new Exception(formattedException);
                _ = LogErrors.LogErrorAsync(ex, formattedException);
            }
        }
        catch (Exception ex)
        {
            _ = LogErrors.LogErrorAsync(ex, "Error in SetSimilarityThreshold_Click");
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
        if (sender is not MenuItem { Header: not null } menuItem)
        {
            return;
        }

        var headerText = menuItem.Header.ToString();
        if (string.IsNullOrEmpty(headerText))
        {
            return;
        }

        try
        {
            var match = Regex.Match(headerText, @"\d+");

            if (!match.Success || !int.TryParse(match.Value, out var size))
            {
                MessageBox.Show("Invalid thumbnail size selected.",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                return;
            }

            App.SettingsManager.ImageWidth = size;
            App.SettingsManager.ImageHeight = size;
            App.SettingsManager.SaveSettings();
        }
        catch (Exception ex)
        {
            _ = LogErrors.LogErrorAsync(ex, "Error in SetThumbnailSize_Click");
        }
    }

    private void UpdateThumbnailSizeMenuChecks()
    {
        var currentSize = App.SettingsManager.ImageWidth;

        foreach (var item in ImageSizeMenu.Items)
        {
            if (item is not MenuItem menuItem) continue;

            var headerText = menuItem.Header?.ToString();
            if (string.IsNullOrEmpty(headerText))
                continue;

            var match = Regex.Match(headerText, @"\d+");

            if (match.Success && int.TryParse(match.Value, out var size))
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
            // Pass the UseImage method as the action
            element.ContextMenu = ButtonFactory.CreateContextMenu(imageData.ImagePath, UseImage);
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
                var isChecked = menuItem.IsChecked == true;
                App.SettingsManager.UseMameDescription = isChecked;
                App.SettingsManager.SaveSettings();

                // Refresh the list immediately after the setting changes
                // Only refresh if MAME data is actually available, otherwise it's pointless
                if (_mameLookup != null && _mameLookup.Count > 0)
                    await RefreshMissingImagesList();
            }
        }
        catch (Exception ex)
        {
            _ = LogErrors.LogErrorAsync(ex, "Error in MenuUseMameDescription_Click");
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
            _ = LogErrors.LogErrorAsync(ex, "Error refreshing missing images list.");
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

            if (!string.IsNullOrEmpty(newPath) && Directory.Exists(newPath))
            {
                // Commit the valid path and update stored last valid path
                textBox.Text = newPath;
                _lastValidImageFolderPath = newPath; // Store the valid path
                textBox.CaretIndex = textBox.Text.Length;
            }
            else
            {
                // Revert to last known good path instead of current invalid text
                textBox.Text = _lastValidImageFolderPath ?? string.Empty;
                if (!string.IsNullOrEmpty(textBox.Text))
                {
                    textBox.CaretIndex = textBox.Text.Length;
                }
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

    private void TxtRomFolder_LostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            var newPath = textBox.Text.Trim();

            if (!string.IsNullOrEmpty(newPath) && Directory.Exists(newPath))
            {
                // Commit the valid path and update stored last valid path
                textBox.Text = newPath;
                _lastValidRomFolderPath = newPath; // Store the valid path
                textBox.CaretIndex = textBox.Text.Length;
            }
            else
            {
                // Revert to last known good path instead of current invalid text
                textBox.Text = _lastValidRomFolderPath ?? string.Empty;
                if (!string.IsNullOrEmpty(textBox.Text))
                {
                    textBox.CaretIndex = textBox.Text.Length;
                }
            }

            UpdateUiStateForFolderPaths();
        }
    }

    private void TxtRomFolder_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && sender is TextBox)
        {
            TxtRomFolder_LostFocus(sender, new RoutedEventArgs(LostFocusEvent, sender));
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

    /// <summary>
    /// Gets the currently displayed image folder path if it is valid and exists.
    /// </summary>
    /// <param name="showWarning">Whether to show a warning message if the path is invalid.</param>
    /// <returns>The validated image folder path, or null/empty if invalid.</returns>
    private string? GetValidatedImageFolderPath(bool showWarning = true)
    {
        var path = TxtImageFolder.Text.Trim();
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
            MessageBox.Show($"The image folder path '{path}' is invalid or does not exist. Please correct it.",
                "Invalid Image Folder", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        return null;
    }

    /// <summary>
    /// Gets the currently displayed ROM folder path if it is valid and exists.
    /// </summary>
    /// <param name="showWarning">Whether to show a warning message if the path is invalid.</param>
    /// <returns>The validated ROM folder path, or null/empty if invalid.</returns>
    private string? GetValidatedRomFolderPath(bool showWarning = true)
    {
        var path = TxtRomFolder.Text.Trim();
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
            MessageBox.Show($"The ROM folder path '{path}' is invalid or does not exist. Please correct it.",
                "Invalid ROM Folder", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        return null;
    }

    private void UpdateUiStateForFolderPaths()
    {
        var romPath = TxtRomFolder.Text.Trim();
        var imagePath = TxtImageFolder.Text.Trim();

        var romPathValid = !string.IsNullOrEmpty(romPath) && Directory.Exists(romPath);
        var imagePathValid = !string.IsNullOrEmpty(imagePath) && Directory.Exists(imagePath);

        // Store valid paths when detected
        if (romPathValid)
        {
            _lastValidRomFolderPath = romPath;
        }

        if (imagePathValid)
        {
            _lastValidImageFolderPath = imagePath;
        }

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
        App.SettingsManager.PropertyChanged -= AppSettingsManagerPropertyChanged;

        // Clean up cancellation tokens and semaphore
        _findSimilarCts?.Dispose();
        _loadMissingCts?.Dispose();
        _findSimilarSemaphore.Dispose();

        base.OnClosed(e);
    }

    public void Dispose()
    {
        _findSimilarCts?.Dispose();
        _findSimilarSemaphore?.Dispose();
        _loadMissingCts?.Dispose();

        GC.SuppressFinalize(this);
    }
}