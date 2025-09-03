using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.ComponentModel;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Documents;
using System.Windows.Input;
using FindRomCover.models;
using Microsoft.Win32;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace FindRomCover;

public partial class MainWindow : INotifyPropertyChanged
{
    private CancellationTokenSource? _loadMissingCts;

    private List<MameManager>? _machines;
    private Dictionary<string, string>? _mameLookup;

    private Task? _currentFindTask;
    private CancellationTokenSource? _findSimilarCts;

    public event PropertyChangedEventHandler? PropertyChanged;
    private string _imageFolderPath;
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

    private bool _isCheckingMissing;

    public bool IsCheckingMissing
    {
        get => _isCheckingMissing;
        set
        {
            if (_isCheckingMissing == value) return;

            _isCheckingMissing = value;
            OnPropertyChanged(nameof(IsCheckingMissing));
        }
    }

    private bool _isFindingSimilar;

    public bool IsFindingSimilar
    {
        get => _isFindingSimilar;
        set
        {
            if (_isFindingSimilar == value) return;

            _isFindingSimilar = value;
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
        _imageWidth = App.Settings.ImageWidth;
        _imageHeight = App.Settings.ImageHeight;
        _selectedSimilarityAlgorithm = App.Settings.SelectedSimilarityAlgorithm;

        // Check for command-line arguments
        var args = Environment.GetCommandLineArgs();
        if (args.Length == 3)
        {
            var imageFolderPath = args[1];
            var romFolderPath = args[2];

            if (Directory.Exists(imageFolderPath) && Directory.Exists(romFolderPath))
            {
                _imageFolderPath = imageFolderPath;
                TxtImageFolder.Text = _imageFolderPath;
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

                _imageFolderPath = "";
                TxtImageFolder.Text = "";
                TxtRomFolder.Text = "";
            }
        }
        else
        {
            _imageFolderPath = "";
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
        App.Settings.PropertyChanged += AppSettings_PropertyChanged;

        // Load _machines and _mameLookup
        LoadMameData();
    }

    private void LoadMameData()
    {
        try
        {
            _machines = MameManager.LoadFromDat();
            _mameLookup = _machines
                .GroupBy(static m => m.MachineName, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(static g => g.Key, static g => g.First().Description, StringComparer.OrdinalIgnoreCase);
        }
        catch
        {
            // Notify developer
            const string contextMessage = "The file 'mame.dat' could not be found in the application folder.";
            _ = LogErrors.LogErrorAsync(null, contextMessage);

            MessageBox.Show(contextMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void UpdateBaseThemeMenuChecks()
    {
        LightTheme.IsChecked = App.Settings.BaseTheme == "Light";
        DarkTheme.IsChecked = App.Settings.BaseTheme == "Dark";
    }

    // Handle settings changes from other parts of the application (e.g., SettingsWindow)
    private void AppSettings_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(Settings.BaseTheme):
                // Base theme is handled by App.ChangeTheme, which applies globally.
                // Ensure menu checks are updated.
                LightTheme.IsChecked = App.Settings.BaseTheme == "Light";
                DarkTheme.IsChecked = App.Settings.BaseTheme == "Dark";
                break;
            case nameof(Settings.AccentColor):
                // Accent color is handled by App.ChangeTheme, which applies globally.
                // Ensure menu checks are updated.
                UpdateAccentColorChecks();
                break;
            case nameof(Settings.ImageWidth):
            case nameof(Settings.ImageHeight):
                // Update local properties and trigger UI update
                _imageWidth = App.Settings.ImageWidth;
                _imageHeight = App.Settings.ImageHeight;
                OnPropertyChanged(nameof(ImageWidth));
                OnPropertyChanged(nameof(ImageHeight));
                UpdateThumbnailSizeMenuChecks();
                break;
            case nameof(Settings.SelectedSimilarityAlgorithm):
                _selectedSimilarityAlgorithm = App.Settings.SelectedSimilarityAlgorithm;
                OnPropertyChanged(nameof(SelectedSimilarityAlgorithm));
                UpdateSimilarityAlgorithmChecks();
                break;
            case nameof(Settings.SimilarityThreshold):
                UpdateSimilarityThresholdChecks();
                break;
            case nameof(Settings.UseMameDescription):
                UpdateMameDescriptionCheck();
                break;
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
        App.ChangeTheme(theme, App.Settings.AccentColor); // Use static App.ChangeTheme
    }

    private void ChangeAccentColor_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem menuItem) return;

        var accent = menuItem.Name.Replace("Accent", "");
        App.ChangeTheme(App.Settings.BaseTheme, accent); // Use static App.ChangeTheme
    }

    private void UpdateAccentColorChecks()
    {
        var currentAccent = App.Settings.AccentColor;
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
        _imageFolderPath = dialog.FolderName;
    }

    private async void BtnCheckForMissingImages_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Cancel any existing operation
            if (_loadMissingCts != null)
            {
                await _loadMissingCts.CancelAsync();
                _loadMissingCts.Dispose();
            }

            _loadMissingCts = new CancellationTokenSource();
            await LoadMissingImagesList(_loadMissingCts.Token);
        }
        catch (Exception ex)
        {
            _ = LogErrors.LogErrorAsync(ex, "Error in BtnCheckForMissingImages_Click");
        }
    }

    private async Task LoadMissingImagesList(CancellationToken cancellationToken = default)
    {
        if ((App.Settings.SupportedExtensions.Length == 0))
        {
            MessageBox.Show("No supported file extensions loaded. Please check file 'settings.xml' or edit them in the Settings menu.", "Warning",
                MessageBoxButton.OK, MessageBoxImage.Warning);

            return;
        }

        var romFolderPath = TxtRomFolder.Text;
        var imageFolderPath = TxtImageFolder.Text;

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

                    var searchPatterns = App.Settings.SupportedExtensions.Select(static ext => "*." + ext).ToArray();
                    var allRomNames = new List<string>();

                    // Process each pattern with cancellation checks
                    foreach (var pattern in searchPatterns)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        try
                        {
                            var files = Directory.EnumerateFiles(romFolderPath, pattern);
                            var names = files.Select(Path.GetFileNameWithoutExtension)
                                .Where(name => name != null)
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
                            if (App.Settings.UseMameDescription &&
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

            // Prepare for new operation
            _findSimilarCts = new CancellationTokenSource();
            var cancellationToken = _findSimilarCts.Token;

            dynamic selectedItem = LstMissingImages.SelectedItem;
            string romName = selectedItem.RomName;
            string searchName = selectedItem.SearchName;

            IsFindingSimilar = true;

            // Create the task and store it
            var findTask = ButtonFactory.CreateSimilarImagesCollection(
                searchName,
                _imageFolderPath,
                App.Settings.SimilarityThreshold,
                SelectedSimilarityAlgorithm,
                cancellationToken
            );
            _currentFindTask = findTask;

            try
            {
                _selectedRomFileName = romName;

                var newSimilarImages = await findTask;

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
                        foreach (var imageData in newSimilarImages)
                        {
                            SimilarImages.Add(imageData);
                        }
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
                // Always reset UI state unless cancelled
                if (!cancellationToken.IsCancellationRequested)
                {
                    IsFindingSimilar = false;
                }

                _currentFindTask = null; // Clear the task reference when done
            }
        }
        catch (Exception ex)
        {
            _ = LogErrors.LogErrorAsync(ex, "Error in LstMissingImages_SelectionChanged");
            IsFindingSimilar = false;
            _currentFindTask = null;
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
        if (string.IsNullOrEmpty(_selectedRomFileName) ||
            string.IsNullOrEmpty(imagePath) ||
            string.IsNullOrEmpty(_imageFolderPath))
        {
            return;
        }

        var newFileName = Path.Combine(_imageFolderPath, _selectedRomFileName + ".png");

        try
        {
            if (ImageProcessor.ConvertAndSaveImage(imagePath, newFileName))
            {
                PlaySound.PlayClickSound();
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
        App.Settings.SelectedSimilarityAlgorithm = algorithm; // Update App.Settings
        App.Settings.SaveSettings(); // Save the settings
    }

    private void UpdateSimilarityAlgorithmChecks()
    {
        foreach (var item in MenuSimilarityAlgorithms.Items)
        {
            if (item is MenuItem menuItem)
            {
                menuItem.IsChecked = menuItem.Header.ToString() == App.Settings.SelectedSimilarityAlgorithm; // Use App.Settings
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
                App.Settings.SimilarityThreshold = rate;
                App.Settings.SaveSettings();
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
        var currentThreshold = App.Settings.SimilarityThreshold;

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

            App.Settings.ImageWidth = size;
            App.Settings.ImageHeight = size;
            App.Settings.SaveSettings();
        }
        catch (Exception ex)
        {
            _ = LogErrors.LogErrorAsync(ex, "Error in SetThumbnailSize_Click");
        }
    }

    private void UpdateThumbnailSizeMenuChecks()
    {
        var currentSize = App.Settings.ImageWidth;

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
        var settingsWindow = new SettingsWindow(App.Settings) // Pass App.Settings
        {
            Owner = this
        };
        settingsWindow.ShowDialog();
    }

    private void TxtImageFolder_TextChanged(object sender, TextChangedEventArgs e)
    {
        // Do NOT update _imageFolderPath here.
        // This allows the user to type freely without prematurely setting the backing field.
        // The _imageFolderPath field should only be updated when a valid path is confirmed
        // (e.g., in LostFocus or when pressing Enter).
        // The current logic of only updating if Directory.Exists is the source of the inconsistency.
        // Simply let the user type. Validation happens on LostFocus or Enter.
    }

    private void LstMissingImages_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Delete)
        {
            RemoveSelectedItem();
            PlaySound.PlayClickSound();
        }
    }

    private void MenuUseMameDescription_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem menuItem)
        {
            // Handle nullable bool properly
            var isChecked = menuItem.IsChecked == true;
            App.Settings.UseMameDescription = isChecked;
            App.Settings.SaveSettings();
        }
    }

    private void TxtImageFolder_LostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            var newPath = textBox.Text.Trim();

            // --- New Logic Starts Here ---
            // Scenario 1: User typed a valid path and moved focus (e.g., clicked elsewhere).
            // Scenario 2: User typed an invalid path and moved focus.
            // Scenario 3: User cleared the textbox and moved focus.

            // If the path in the textbox is valid, update the internal field.
            if (!string.IsNullOrEmpty(newPath) && Directory.Exists(newPath))
            {
                // Commit the valid path
                _imageFolderPath = newPath;
                // Ensure the textbox reflects the committed path (handles trailing spaces etc.)
                textBox.Text = newPath;
                // Optional: Move caret to the end if text was trimmed
                textBox.CaretIndex = textBox.Text.Length;
            }
            else
            {
                // If the path is invalid (doesn't exist) or empty, revert the textbox
                // to show the last known good path stored in _imageFolderPath.
                // This gives clear visual feedback that the typed path was not accepted.
                textBox.Text = _imageFolderPath;
                // Optional: Move caret to the end
                if (!string.IsNullOrEmpty(_imageFolderPath))
                {
                    textBox.CaretIndex = textBox.Text.Length;
                }
            }
            // --- New Logic Ends Here ---
        }
    }

    private void UpdateMameDescriptionCheck()
    {
        MenuUseMameDescription.IsChecked = App.Settings.UseMameDescription;
    }

    private void BtnRemoveSelectedItem_Click(object sender, RoutedEventArgs e)
    {
        RemoveSelectedItem();
        SimilarImages.Clear();
        PlaySound.PlayClickSound();
    }

    private void TxtImageFolder_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && sender is TextBox)
        {
            // Trigger the same logic as LostFocus when Enter is pressed
            TxtImageFolder_LostFocus(sender, new RoutedEventArgs(LostFocusEvent, sender));
            // Prevent the Enter key from being processed further (e.g., adding a newline)
            e.Handled = true;
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        App.Settings.PropertyChanged -= AppSettings_PropertyChanged;

        // Clean up cancellation tokens
        _findSimilarCts?.Dispose();
        _loadMissingCts?.Dispose();

        base.OnClosed(e);
    }
}