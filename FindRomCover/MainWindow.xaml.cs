using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using FindRomCover.Managers;
using FindRomCover.Models;
using FindRomCover.Services;
using Microsoft.Win32;
using Microsoft.Web.WebView2.Core;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace FindRomCover;

public partial class MainWindow : INotifyPropertyChanged, IDisposable
{
    private CancellationTokenSource? _loadMissingCts;
    private Dictionary<string, string>? _mameLookup;
    private CancellationTokenSource? _findSimilarCts;
    private readonly SemaphoreSlim _findSimilarSemaphore = new(1, 1);
    private Task? _findSimilarTask;
    private string _selectedRomFileName = string.Empty;
    private bool _disposed;
    private CoreWebView2Environment? _webViewEnv;
    private ImageFolderWatcher? _imageFolderWatcher;
    private string? _watchedFolderPath;
    private SystemTrayIcon? _systemTrayIcon;
    private bool _isExiting;

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<ImageData> SimilarImages { get; set; } = [];
    public ObservableCollection<ImageData> PanelImages { get; set; } = [];
    public ObservableCollection<MissingImageItem> MissingImages { get; set; } = [];

    public bool IsCheckingMissing
    {
        get;
        set
        {
            if (field == value) return;

            field = value;
            OnPropertyChanged();
        }
    }

    public bool IsFindingSimilar
    {
        get;
        set
        {
            if (field == value) return;

            field = value;
            OnPropertyChanged();
        }
    }

    public bool HasSearchedSimilar
    {
        get;
        set
        {
            if (field == value) return;

            field = value;
            OnPropertyChanged();
        }
    }

    public bool IsSearching
    {
        get;
        set
        {
            if (field == value) return;

            field = value;
            OnPropertyChanged();
        }
    }

    public bool HasSearchedApi
    {
        get;
        set
        {
            if (field == value) return;

            field = value;
            OnPropertyChanged();
        }
    }

    public SettingsManager Settings { get; }

    public ICommand CheckForMissingImagesCommand { get; }
    public ICommand ExitCommand { get; }

    public MainWindow(SettingsManager settingsManager, string? startupImageFolder = null, string? startupRomFolder = null)
    {
        Settings = settingsManager;
        InitializeComponent();
        DataContext = this;

        CheckForMissingImagesCommand = new DelegateCommand(
            async void (_) =>
            {
                try
                {
                    await RefreshMissingImagesListAsync();
                }
                catch (Exception ex)
                {
                    LogService.Error(ex, "Error in CheckForMissingImagesCommand");
                }
            },
            _ => BtnCheckForMissingImages?.IsEnabled ?? false);
        ExitCommand = new DelegateCommand(_ => Close());

        if (!string.IsNullOrEmpty(startupImageFolder))
        {
            TxtImageFolder.Text = startupImageFolder;
        }

        if (!string.IsNullOrEmpty(startupRomFolder))
        {
            TxtRomFolder.Text = startupRomFolder;
        }

        UpdateThumbnailSizeMenuChecks();
        UpdateSimilarityAlgorithmChecks();
        UpdateSimilarityThresholdChecks();
        UpdateAccentColorChecks();
        UpdateBaseThemeMenuChecks();
        UpdateMameDescriptionCheck();

        Settings.PropertyChanged += AppSettingsManagerPropertyChangedAsync;
        Closing += OnWindowClosing;
        Loaded += MainWindow_LoadedAsync;
        StateChanged += OnWindowStateChanged;

        InitializeNotifyIcon();

        _ = LoadMameDataAsync();
        UpdateUiStateForFolderPaths();
    }

    private void InitializeNotifyIcon()
    {
        try
        {
            _systemTrayIcon = new SystemTrayIcon();
            _systemTrayIcon.Initialize();
            _systemTrayIcon.RestoreRequested += RestoreFromTray;
            _systemTrayIcon.ExitRequested += ExitApplication;
        }
        catch (Exception ex) { LogService.Error(ex, "Error in InitializeNotifyIcon"); }
    }

    private void OnWindowStateChanged(object? sender, EventArgs e)
    {
        try
        {
            if (WindowState == WindowState.Minimized)
            {
                MinimizeToTray();
            }
        }
        catch (Exception ex) { LogService.Error(ex, "Error in OnWindowStateChanged"); }
    }

    private void MinimizeToTray()
    {
        try
        {
            if (_systemTrayIcon == null) return;

            _systemTrayIcon.Visible = true;
            Hide();
            _systemTrayIcon.ShowBalloonTip("FindRomCover", "Application minimized to tray.");
        }
        catch (Exception ex) { LogService.Error(ex, "Error in MinimizeToTray"); }
    }

    private void RestoreFromTray()
    {
        try
        {
            Show();
            WindowState = WindowState.Normal;
            Activate();
            _systemTrayIcon?.Visible = false;
        }
        catch (Exception ex) { LogService.Error(ex, "Error in RestoreFromTray"); }
    }

    private void ExitApplication()
    {
        try
        {
            _isExiting = true;
            _systemTrayIcon?.Dispose();
            _systemTrayIcon = null;
            Application.Current.Shutdown();
        }
        catch (Exception ex) { LogService.Error(ex, "Error in ExitApplication"); }
    }

    private async void MainWindow_LoadedAsync(object sender, RoutedEventArgs e)
    {
        try
        {
            LstMissingImages.PreviewKeyDown += LstMissingImages_PreviewKeyDown;

            await InitializeWebViewsAsync();

            if (!string.IsNullOrEmpty(TxtRomFolder.Text) && !string.IsNullOrEmpty(TxtImageFolder.Text))
            {
                await RefreshMissingImagesListAsync();
            }
        }
        catch (Exception ex)
        {
            LogService.Error(ex, "Error in MainWindow_LoadedAsync");
        }
    }

    private async Task InitializeWebViewsAsync()
    {
        try
        {
            _webViewEnv = await CreateWebViewEnvironmentAsync();

            try
            {
                await GoogleWebView.EnsureCoreWebView2Async(_webViewEnv);
                GoogleWebView.NavigationCompleted += (_, _) =>
                {
                    var source = GoogleWebView.CoreWebView2?.Source;
                    if (!string.IsNullOrEmpty(source) && !source.Equals("about:blank", StringComparison.OrdinalIgnoreCase))
                    {
                        IsSearching = false;
                        StatusMessage.Text = "Google web search loaded.";
                    }
                };
            }
            catch (Exception ex)
            {
                LogService.Error(ex, "Failed to initialize Google WebView2");
            }

            try
            {
                await BingWebView.EnsureCoreWebView2Async(_webViewEnv);
                BingWebView.NavigationCompleted += (_, _) =>
                {
                    var source = BingWebView.CoreWebView2?.Source;
                    if (!string.IsNullOrEmpty(source) && !source.Equals("about:blank", StringComparison.OrdinalIgnoreCase))
                    {
                        IsSearching = false;
                        StatusMessage.Text = "Bing web search loaded.";
                    }
                };
            }
            catch (Exception ex)
            {
                LogService.Error(ex, "Failed to initialize Bing WebView2");
            }
        }
        catch (Exception ex)
        {
            LogService.Error(ex, "Failed to initialize WebView2 environment");
        }
    }

    private static Task<CoreWebView2Environment> CreateWebViewEnvironmentAsync()
    {
        var userDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FindRomCover", "WebView2Data");

        try
        {
            if (!Directory.Exists(userDataFolder))
                Directory.CreateDirectory(userDataFolder);
        }
        catch
        {
            userDataFolder = null;
        }

        return CoreWebView2Environment.CreateAsync(null, userDataFolder);
    }

    private async Task<bool> EnsureWebViewReadyAsync(Microsoft.Web.WebView2.Wpf.WebView2 webView)
    {
        if (webView.CoreWebView2 != null)
            return true;

        try
        {
            if (_webViewEnv == null)
            {
                _webViewEnv = await CreateWebViewEnvironmentAsync();
            }

            await webView.EnsureCoreWebView2Async(_webViewEnv);
            return webView.CoreWebView2 != null;
        }
        catch (Exception ex)
        {
            LogService.Error(ex, "Failed to lazily initialize WebView2");
            return false;
        }
    }

    private async Task LoadMameDataAsync()
    {
        try
        {
            var machines = await Task.Run(MameManager.LoadFromDat);
            _mameLookup = machines
                .GroupBy(static m => m.MachineName, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(static g => g.Key, static g => g.First().Description, StringComparer.OrdinalIgnoreCase);
            ToggleMameDescriptions.IsEnabled = true;
        }
        catch (Exception ex)
        {
            _mameLookup = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            ToggleMameDescriptions.IsEnabled = false;
            ToggleMameDescriptions.IsChecked = false;
            LogService.Error(ex, "Failed to load MAME data");
        }
    }

    private async void AppSettingsManagerPropertyChangedAsync(object? sender, PropertyChangedEventArgs e)
    {
        try
        {
            switch (e.PropertyName)
            {
                case nameof(SettingsManager.BaseTheme):
                    UpdateBaseThemeMenuChecks();
                    break;
                case nameof(SettingsManager.AccentColor):
                    UpdateAccentColorChecks();
                    break;
                case nameof(SettingsManager.ImageWidth):
                case nameof(SettingsManager.ImageHeight):
                    UpdateThumbnailSizeMenuChecks();
                    break;
                case nameof(SettingsManager.SelectedSimilarityAlgorithm):
                    UpdateSimilarityAlgorithmChecks();
                    break;
                case nameof(SettingsManager.SimilarityThreshold):
                    UpdateSimilarityThresholdChecks();
                    break;
                case nameof(SettingsManager.UseMameDescriptions):
                    UpdateMameDescriptionCheck();
                    if (_mameLookup is { Count: > 0 })
                        await RefreshMissingImagesListAsync();
                    break;
            }
        }
        catch (Exception ex)
        {
            LogService.Error(ex, "Error in AppSettings_PropertyChanged");
        }
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void ChangeBaseTheme_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (sender is not MenuItem menuItem) return;

            var theme = menuItem.Name == "LightTheme" ? "Light" : "Dark";
            App.ChangeTheme(theme, Settings.AccentColor);
        }
        catch (Exception ex) { LogService.Error(ex, "Error in ChangeBaseTheme_Click"); }
    }

    private void ChangeAccentColor_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (sender is not MenuItem menuItem) return;

            var accent = menuItem.Name.Replace("Accent", "");
            App.ChangeTheme(Settings.BaseTheme, accent);
        }
        catch (Exception ex) { LogService.Error(ex, "Error in ChangeAccentColor_Click"); }
    }

    private void UpdateAccentColorChecks()
    {
        try
        {
            var currentAccent = Settings.AccentColor;
            foreach (var item in MenuAccentColors.Items)
            {
                if (item is not MenuItem { Header: not null } menuItem) continue;

                if (menuItem.Header is StackPanel sp)
                {
                    var tb = sp.Children.OfType<TextBlock>().FirstOrDefault();
                    if (tb != null)
                    {
                        menuItem.IsChecked = tb.Text == currentAccent;
                    }
                }
                else
                {
                    menuItem.IsChecked = menuItem.Name.Replace("Accent", "") == currentAccent;
                }
            }
        }
        catch (Exception ex) { LogService.Error(ex, "Error in UpdateAccentColorChecks"); }
    }

    private void UpdateBaseThemeMenuChecks()
    {
        try
        {
            LightTheme.IsChecked = Settings.BaseTheme == "Light";
            DarkTheme.IsChecked = Settings.BaseTheme == "Dark";
        }
        catch (Exception ex) { LogService.Error(ex, "Error in UpdateBaseThemeMenuChecks"); }
    }

    private void UpdateMameDescriptionCheck()
    {
        try
        {
            ToggleMameDescriptions.IsChecked = Settings.UseMameDescriptions;
        }
        catch (Exception ex) { LogService.Error(ex, "Error in UpdateMameDescriptionCheck"); }
    }

    private void DonateButton_Click(object sender, RoutedEventArgs e)
    {
        try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("https://www.purelogiccode.com/donate") { UseShellExecute = true }); }
        catch (Exception ex)
        {
            MessageBox.Show($"Unable to open the donation link: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            LogService.Error(ex, "Error opening donation link");
        }
    }

    private void ShowAboutWindow_Click(object sender, RoutedEventArgs e)
    {
        try { new AboutWindow { Owner = this }.ShowDialog(); }
        catch (Exception ex) { LogService.Error(ex, "Error in ShowAboutWindow_Click"); }
    }

    private async void CheckForUpdates_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var updateInfo = await UpdateCheckService.CheckForUpdateAsync();

            if (updateInfo is { IsUpdateAvailable: true })
            {
                var choice = MessageBox.Show(
                    $"A new version of FindRomCover is available!\n\n" +
                    $"Current: {updateInfo.CurrentVersion}\n" +
                    $"Latest: {updateInfo.LatestVersion}\n\n" +
                    "Would you like to go to the download page?",
                    "Update Available", MessageBoxButton.YesNo, MessageBoxImage.Information);

                if (choice == MessageBoxResult.Yes)
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(updateInfo.ReleaseUrl) { UseShellExecute = true });
                }
            }
            else
            {
                MessageBox.Show(
                    $"You are running the latest version (v{updateInfo.CurrentVersion}).",
                    "No Updates Available", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Unable to check for updates: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            LogService.Error(ex, "Error checking for updates");
        }
    }

    private void ApiSettings_Click(object sender, RoutedEventArgs e)
    {
        try { new ApiSettingsWindow(Settings) { Owner = this }.ShowDialog(); }
        catch (Exception ex) { LogService.Error(ex, "Error in ApiSettings_Click"); }
    }

    private void ToggleDebugWindow_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (App.LogWindow == null) return;

            if (ToggleDebugWindow.IsChecked) App.LogWindow.Show(); else App.LogWindow.Hide();
        }
        catch (Exception ex) { LogService.Error(ex, "Error in ToggleDebugWindow_Click"); }
    }

    private void ToggleMameDescriptions_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Settings.UseMameDescriptions = ToggleMameDescriptions.IsChecked;
            Settings.SaveSettings();
        }
        catch (Exception ex) { LogService.Error(ex, "Error in ToggleMameDescriptions_Click"); }
    }

    private void OnWindowClosing(object? sender, CancelEventArgs e)
    {
        if (_isExiting)
        {
            // Allow close when explicitly exiting
            try { _findSimilarCts?.Cancel(); }
            catch
            {
                // ignored
            }

            try { _loadMissingCts?.Cancel(); }
            catch
            {
                // ignored
            }

            return;
        }

        // Allow the window to close normally (X button or Alt+F4)
        try { _findSimilarCts?.Cancel(); }
        catch
        {
            // ignored
        }

        try { _loadMissingCts?.Cancel(); }
        catch
        {
            // ignored
        }
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        try { ExitApplication(); }
        catch (Exception ex) { LogService.Error(ex, "Error in Exit_Click"); }
    }

    private void BtnBrowseRomFolder_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var dialog = new OpenFolderDialog { Title = "Select the folder where your ROM or ISO files are stored." };
            if (dialog.ShowDialog() == true)
            {
                TxtRomFolder.Text = dialog.FolderName;
                UpdateUiStateForFolderPaths();
            }
        }
        catch (Exception ex) { LogService.Error(ex, "Error in BtnBrowseRomFolder_Click"); }
    }

    private void BtnBrowseImageFolder_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var dialog = new OpenFolderDialog { Title = "Select the folder where your image files are stored." };
            if (dialog.ShowDialog() != true) return;

            TxtImageFolder.Text = dialog.FolderName;
            UpdateUiStateForFolderPaths();
            Settings.LastImageFolder = dialog.FolderName;
            Settings.SaveSettings();
        }
        catch (Exception ex) { LogService.Error(ex, "Error in BtnBrowseImageFolder_Click"); }
    }

    private async void BtnCheckForMissingImages_ClickAsync(object sender, RoutedEventArgs e)
    {
        try { await RefreshMissingImagesListAsync(); }
        catch (Exception ex) { LogService.Error(ex, "Error in BtnCheckForMissingImages_Click"); }
    }

    private async Task RefreshMissingImagesListAsync()
    {
        if (_loadMissingCts != null)
        {
            await _loadMissingCts.CancelAsync();
            _loadMissingCts.Dispose();
            _loadMissingCts = null;
        }

        var cts = new CancellationTokenSource();
        _loadMissingCts = cts;
        try
        {
            await LoadMissingImagesListAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex) { LogService.Error(ex, "Error refreshing missing images list."); }
        finally
        {
            if (_loadMissingCts == cts)
            {
                _loadMissingCts = null;
            }
            cts.Dispose();
        }
    }

    private async Task LoadMissingImagesListAsync(CancellationToken cancellationToken = default)
    {
        if (Settings.SupportedExtensions.Count == 0)
        {
            MessageBox.Show("No supported file extensions loaded. Please check settings.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var romFolderPath = GetValidatedRomFolderPath();
        var imageFolderPath = GetValidatedImageFolderPath();

        if (string.IsNullOrEmpty(romFolderPath) || string.IsNullOrEmpty(imageFolderPath))
        {
            MessageBox.Show("Please select both ROM and Image folders.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        IsCheckingMissing = true;
        try
        {
            var missingFiles = await Task.Run(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                var allRomNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var supportedExtensionsSet = new HashSet<string>(
                    Settings.SupportedExtensions.Select(static ext => "." + ext), StringComparer.OrdinalIgnoreCase);

                var files = Directory.EnumerateFiles(romFolderPath, "*.*", new EnumerationOptions { IgnoreInaccessible = true, RecurseSubdirectories = true });

                foreach (var file in files)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (supportedExtensionsSet.Contains(Path.GetExtension(file)))
                    {
                        var fileName = Path.GetFileNameWithoutExtension(file);
                        if (!string.IsNullOrEmpty(fileName)) allRomNames.Add(fileName);
                    }
                }

                var missing = new List<(string RomName, string SearchName)>();
                var processedCount = 0;
                foreach (var romName in allRomNames)
                {
                    if (++processedCount % 100 == 0)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        await Task.Yield();
                    }

                    if (FindCorrespondingImage(romName, imageFolderPath) == null)
                    {
                        if (Settings.UseMameDescriptions && _mameLookup != null &&
                            _mameLookup.TryGetValue(romName, out var description) && !string.IsNullOrEmpty(description))
                            missing.Add((romName, description));
                        else
                            missing.Add((romName, romName));
                    }
                }

                cancellationToken.ThrowIfCancellationRequested();
                return missing.OrderBy(static x => x.RomName).ToList();
            }, cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();
            MissingImages.Clear();
            SimilarImages.Clear();

            foreach (var item in missingFiles.Select(static mf => new MissingImageItem(mf.RomName, mf.SearchName)))
                MissingImages.Add(item);

            UpdateMissingCount();
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error checking for missing images: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            LogService.Error(ex, "Error checking for missing images");
        }
        finally { IsCheckingMissing = false; }
    }

    private static string? FindCorrespondingImage(string fileNameWithoutExtension, string imageFolderPath)
    {
        foreach (var ext in new[] { ".png", ".jpg", ".jpeg" })
        {
            var imagePath = Path.Combine(imageFolderPath, fileNameWithoutExtension + ext);
            if (File.Exists(imagePath)) return imagePath;
        }

        return null;
    }

    private void RemoveSelectedItem(int? index = null)
    {
        var removeIndex = index ?? LstMissingImages.SelectedIndex;
        if (removeIndex < 0 || removeIndex >= MissingImages.Count) return;

        try
        {
            MissingImages.RemoveAt(removeIndex);
            if (MissingImages.Count > 0)
            {
                var newIndex = Math.Min(removeIndex, MissingImages.Count - 1);
                LstMissingImages.SelectedIndex = newIndex;
                LstMissingImages.ScrollIntoView(MissingImages[newIndex]);
            }
            else { LblLocalSearchQuery.Content = null; }
        }
        catch (Exception ex) { LogService.Error(ex, "Error in RemoveSelectedItem"); }

        UpdateMissingCount();
    }

    private void UpdateMissingCount()
    {
        try { LabelMissingRoms.Content = AppConstants.Messages.MissingCoversPrefix + MissingImages.Count; }
        catch (Exception ex) { LogService.Error(ex, "Error in UpdateMissingCount"); }
    }

    private void BtnRemoveSelectedItem_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            RemoveSelectedItem();
            SimilarImages.Clear();
            App.AudioService.PlayClickSound();
        }
        catch (Exception ex) { LogService.Error(ex, "Error in BtnRemoveSelectedItem_Click"); }
    }

    private void RemoveItemFromList_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            RemoveSelectedItem();
            App.AudioService.PlayClickSound();
        }
        catch (Exception ex) { LogService.Error(ex, "Error in RemoveItemFromList_Click"); }
    }

    private void CopyFileName_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (LstMissingImages.SelectedItem is MissingImageItem item)
            {
                try { Clipboard.SetText(item.RomName); }
                catch
                {
                    // ignored
                }
            }
        }
        catch (Exception ex) { LogService.Error(ex, "Error in CopyFileName_Click"); }
    }

    private async void DeleteCorrespondingRom_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (LstMissingImages.SelectedItem is not MissingImageItem selectedItem) return;

            var romFolderPath = GetValidatedRomFolderPath();
            if (string.IsNullOrEmpty(romFolderPath)) return;

            var romFilePath = await Task.Run(() => FindCorrespondingRomFile(selectedItem.RomName, romFolderPath, Settings.SupportedExtensions));
            if (romFilePath == null)
            {
                MessageBox.Show($"Could not find a ROM or ISO file for '{selectedItem.RomName}' in the ROM folder.",
                    "File Not Found", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"Are you sure you want to permanently delete this file?\n\n{romFilePath}\n\nThis action cannot be undone.",
                "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                File.Delete(romFilePath);
                LogService.Information($"Deleted ROM file: {romFilePath}");
                RemoveSelectedItem();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to delete file: {ex.Message}", "Delete Error", MessageBoxButton.OK, MessageBoxImage.Error);
                LogService.Error(ex, $"Error deleting ROM file: {romFilePath}");
            }
        }
        catch (Exception ex)
        {
            LogService.Error(ex, "Error deleting ROM file");
        }
    }

    private static readonly HashSet<string> NonRomExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".dummy", ".bat", ".url", ".lnk", ".exe", ".cmd", ".txt", ".nfo", ".xml", ".json", ".ini", ".cfg", ".log", ".dat"
    };

    private static string? FindCorrespondingRomFile(string fileNameWithoutExtension, string romFolderPath, List<string> supportedExtensions)
    {
        var extensionsWithDot = supportedExtensions
            .Select(static ext => ext.StartsWith('.') ? ext : "." + ext)
            .Where(static ext => !NonRomExtensions.Contains(ext))
            .ToArray();

        foreach (var ext in extensionsWithDot)
        {
            var romPath = Path.Combine(romFolderPath, fileNameWithoutExtension + ext);
            if (File.Exists(romPath)) return romPath;
        }

        // Try case-insensitive search
        if (!Directory.Exists(romFolderPath)) return null;

        var extensionsSet = new HashSet<string>(extensionsWithDot, StringComparer.OrdinalIgnoreCase);
        foreach (var file in Directory.EnumerateFiles(romFolderPath, "*.*", new EnumerationOptions { IgnoreInaccessible = true, RecurseSubdirectories = true }))
        {
            if (extensionsSet.Contains(Path.GetExtension(file)) &&
                string.Equals(Path.GetFileNameWithoutExtension(file), fileNameWithoutExtension, StringComparison.OrdinalIgnoreCase))
            {
                return file;
            }
        }

        return null;
    }

    private void EditExtensions_Click(object sender, RoutedEventArgs e)
    {
        try { new SettingsWindow(Settings) { Owner = this }.ShowDialog(); }
        catch (Exception ex) { LogService.Error(ex, "Error in EditExtensions_Click"); }
    }

    private void SetSimilarityAlgorithm_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (sender is not MenuItem menuItem) return;

            Settings.SelectedSimilarityAlgorithm = menuItem.Header.ToString() ?? "Jaro-Winkler Distance";
            Settings.SaveSettings();
        }
        catch (Exception ex) { LogService.Error(ex, "Error in SetSimilarityAlgorithm_Click"); }
    }

    private void UpdateSimilarityAlgorithmChecks()
    {
        try
        {
            foreach (var item in MenuSimilarityAlgorithms.Items)
            {
                if (item is MenuItem menuItem)
                {
                    menuItem.IsChecked = menuItem.Header.ToString() == Settings.SelectedSimilarityAlgorithm;
                }
            }
        }
        catch (Exception ex) { LogService.Error(ex, "Error in UpdateSimilarityAlgorithmChecks"); }
    }

    private void SetSimilarityThreshold_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (sender is not MenuItem clickedItem) return;

            var headerText = clickedItem.Header.ToString()?.Replace("%", "") ?? "70";
            if (double.TryParse(headerText, out var rate))
            {
                Settings.SimilarityThreshold = rate;
                Settings.SaveSettings();
            }
        }
        catch (Exception ex) { LogService.Error(ex, "Error in SetSimilarityThreshold_Click"); }
    }

    private void UpdateSimilarityThresholdChecks()
    {
        try
        {
            var currentThreshold = Settings.SimilarityThreshold;
            foreach (var item in MySimilarityMenu.Items)
            {
                if (item is not MenuItem menuItem) continue;

                var thresholdString = menuItem.Header.ToString()?.Replace("%", "") ?? "70";
                if (double.TryParse(thresholdString, NumberStyles.Any, CultureInfo.InvariantCulture, out var menuItemThreshold))
                {
                    menuItem.IsChecked = Math.Abs(menuItemThreshold - currentThreshold) < 0.001;
                }
            }
        }
        catch (Exception ex) { LogService.Error(ex, "Error in UpdateSimilarityThresholdChecks"); }
    }

    private void SetThumbnailSize_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (sender is not MenuItem menuItem) return;
            if (menuItem.Tag is not int size && !int.TryParse(menuItem.Tag?.ToString(), out size)) return;

            Settings.ImageWidth = size;
            Settings.ImageHeight = size;
            Settings.SaveSettings();
        }
        catch (Exception ex) { LogService.Error(ex, "Error in SetThumbnailSize_Click"); }
    }

    private void UpdateThumbnailSizeMenuChecks()
    {
        try
        {
            var currentWidth = Settings.ImageWidth;
            foreach (var item in ImageSizeMenu.Items)
            {
                if (item is not MenuItem menuItem) continue;

                if (menuItem.Tag is int size || int.TryParse(menuItem.Tag?.ToString(), out size))
                {
                    menuItem.IsChecked = size == currentWidth;
                }
            }
        }
        catch (Exception ex) { LogService.Error(ex, "Error in UpdateThumbnailSizeMenuChecks"); }
    }

    private void LstMissingImages_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        try
        {
            if (e is { Key: Key.Delete, IsRepeat: false })
            {
                RemoveSelectedItem();
                App.AudioService.PlayClickSound();
                e.Handled = true;
            }
        }
        catch (Exception ex) { LogService.Error(ex, "Error in LstMissingImages_PreviewKeyDown"); }
    }

    private void TxtRomFolder_TextChanged(object sender, TextChangedEventArgs e)
    {
        try { UpdateUiStateForFolderPaths(); }
        catch (Exception ex) { LogService.Error(ex, "Error in TxtRomFolder_TextChanged"); }
    }

    private void TxtImageFolder_TextChanged(object sender, TextChangedEventArgs e)
    {
        try { UpdateUiStateForFolderPaths(); }
        catch (Exception ex) { LogService.Error(ex, "Error in TxtImageFolder_TextChanged"); }
    }

    private void TxtRomFolder_LostFocus(object sender, RoutedEventArgs e)
    {
        try { UpdateUiStateForFolderPaths(); }
        catch (Exception ex) { LogService.Error(ex, "Error in TxtRomFolder_LostFocus"); }
    }

    private void TxtImageFolder_LostFocus(object sender, RoutedEventArgs e)
    {
        try { UpdateUiStateForFolderPaths(); }
        catch (Exception ex) { LogService.Error(ex, "Error in TxtImageFolder_LostFocus"); }
    }

    private void TxtRomFolder_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        try
        {
            if (e.Key == Key.Enter)
            {
                UpdateUiStateForFolderPaths();
                e.Handled = true;
            }
        }
        catch (Exception ex) { LogService.Error(ex, "Error in TxtRomFolder_PreviewKeyDown"); }
    }

    private void TxtImageFolder_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        try
        {
            if (e.Key == Key.Enter)
            {
                UpdateUiStateForFolderPaths();
                e.Handled = true;
            }
        }
        catch (Exception ex) { LogService.Error(ex, "Error in TxtImageFolder_PreviewKeyDown"); }
    }

    private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        try
        {
            if (e.Key == Key.F8)
            {
                var filePath = ScreenshotService.CaptureActiveWindow();
                if (filePath is not null)
                {
                    StatusMessage.Text = $"Screenshot saved: {filePath}";
                }
                else
                {
                    StatusMessage.Text = "Screenshot failed. Check the log for details.";
                }

                e.Handled = true;
            }
        }
        catch (Exception ex) { LogService.Error(ex, "Error in MainWindow_PreviewKeyDown"); }
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
        if (string.IsNullOrEmpty(path)) return null;
        if (Directory.Exists(path)) return path;

        if (showWarning) MessageBox.Show($"The {folderType.ToLowerInvariant()} folder path '{path}' is invalid or does not exist.", $"Invalid {folderType} Folder", MessageBoxButton.OK, MessageBoxImage.Warning);
        return null;
    }

    private void UpdateUiStateForFolderPaths()
    {
        try
        {
            var romPathValid = !string.IsNullOrEmpty(TxtRomFolder.Text.Trim()) && Directory.Exists(TxtRomFolder.Text.Trim());
            var imagePathValid = !string.IsNullOrEmpty(TxtImageFolder.Text.Trim()) && Directory.Exists(TxtImageFolder.Text.Trim());
            BtnCheckForMissingImages.IsEnabled = romPathValid && imagePathValid;
            LstMissingImages.IsEnabled = romPathValid && imagePathValid;
            if (!romPathValid || !imagePathValid)
            {
                MissingImages.Clear();
                SimilarImages.Clear();
                UpdateMissingCount();
            }

            StartImageFolderWatcher(imagePathValid ? TxtImageFolder.Text.Trim() : null);

            CommandManager.InvalidateRequerySuggested();
        }
        catch (Exception ex) { LogService.Error(ex, "Error in UpdateUiStateForFolderPaths"); }
    }

    private void StartImageFolderWatcher(string? folderPath)
    {
        if (string.Equals(_watchedFolderPath, folderPath, StringComparison.OrdinalIgnoreCase))
            return;

        _watchedFolderPath = folderPath;

        _imageFolderWatcher?.Stop();

        if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath))
        {
            LogService.Information($"StartImageFolderWatcher: skipped (path='{folderPath}', exists={Directory.Exists(folderPath ?? "")})");
            return;
        }

        _imageFolderWatcher ??= new ImageFolderWatcher();
        _imageFolderWatcher.ImageFound -= OnImageFolderImageFound;
        _imageFolderWatcher.ImageFound += OnImageFolderImageFound;
        _imageFolderWatcher.ConversionFailed -= OnImageFolderConversionFailed;
        _imageFolderWatcher.ConversionFailed += OnImageFolderConversionFailed;
        _imageFolderWatcher.Start(folderPath);
        LogService.Information($"StartImageFolderWatcher: watcher started for '{folderPath}'");
    }

    private void OnImageFolderImageFound(string fileNameWithoutExtension)
    {
        Dispatcher.Invoke(() =>
        {
            var index = MissingImages.ToList().FindIndex(m =>
                string.Equals(m.RomName, fileNameWithoutExtension, StringComparison.OrdinalIgnoreCase));

            if (index < 0)
            {
                LogService.Information($"ImageFolderWatcher: image '{fileNameWithoutExtension}' does not match any missing ROM name — skipping");
                StatusMessage.Text = $"Image '{fileNameWithoutExtension}.png' was saved but doesn't match any missing ROM name.";
                return;
            }

            RemoveSelectedItem(index);
            LogService.Information($"Auto-removed '{fileNameWithoutExtension}' from missing images.");
        });
    }

    private void OnImageFolderConversionFailed(string filePath, string errorMessage)
    {
        Dispatcher.Invoke(() =>
        {
            MessageBox.Show(
                $"Failed to convert image:\n\n{Path.GetFileName(filePath)}\n\n{errorMessage}",
                "Image Conversion Failed",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        });
    }

    private void SearchTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        try
        {
            if (StatusSearchEngine == null) return;

            var tab = SearchTabControl.SelectedIndex;
            StatusSearchEngine.Text = tab switch
            {
                0 => "Local Files",
                1 => "Google Web",
                2 => "Bing Web",
                3 => "Google API",
                _ => "Unknown"
            };

            if (tab == 3 && string.IsNullOrWhiteSpace(Settings.GoogleKey))
            {
                MessageBox.Show(
                    "A Google API key is required to use the Google API search.\n\nPlease enter your API key in the settings window that will open.",
                    "API Key Required",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                var apiSettingsWindow = new ApiSettingsWindow(Settings) { Owner = this };
                apiSettingsWindow.ShowDialog();

                if (string.IsNullOrWhiteSpace(Settings.GoogleKey))
                {
                    StatusMessage.Text = "API key not set. Google API search is unavailable.";
                    return;
                }
            }

            // If an item is already selected, trigger the search for the new tab
            if (LstMissingImages.SelectedItem is MissingImageItem selectedItem)
            {
                TriggerActiveTabSearch(selectedItem.SearchName);
            }
        }
        catch (Exception ex) { LogService.Error(ex, "Error in SearchTabControl_SelectionChanged"); }
    }

    protected override void OnClosed(EventArgs e)
    {
        Dispose();
        base.OnClosed(e);
    }

    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;
        Settings.PropertyChanged -= AppSettingsManagerPropertyChangedAsync;

        _findSimilarCts?.Cancel();
        _loadMissingCts?.Cancel();

        try { _findSimilarTask?.Wait(TimeSpan.FromSeconds(2)); }
        catch { /* ignored - task was cancelled */ }

        _findSimilarCts?.Dispose();
        _findSimilarCts = null;
        _loadMissingCts?.Dispose();
        _loadMissingCts = null;

        _findSimilarSemaphore.Dispose();
        _imageFolderWatcher?.Dispose();
        _systemTrayIcon?.Dispose();

        if (CheckForMissingImagesCommand is IDisposable disposableCheckCommand)
            disposableCheckCommand.Dispose();
        if (ExitCommand is IDisposable disposableExitCommand)
            disposableExitCommand.Dispose();

        GC.SuppressFinalize(this);
    }
}
