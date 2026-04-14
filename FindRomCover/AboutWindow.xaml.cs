using System.Windows;
using System.Diagnostics;
using System.Windows.Navigation;
using System.Reflection;
using FindRomCover.Services;
using MessageBox = System.Windows.MessageBox;

namespace FindRomCover;

public partial class AboutWindow
{
    public AboutWindow()
    {
        try
        {
            InitializeComponent();
            App.ApplyThemeToWindow(this);
            AppVersionTextBlock.Text = ApplicationVersion;
        }
        catch (Exception ex)
        {
            _ = ErrorLogger.LogAsync(ex, "Error initializing AboutWindow");
            throw; // Re-throw to prevent partial initialization
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Close();
        }
        catch (Exception ex)
        {
            _ = ErrorLogger.LogAsync(ex, "Error in CloseButton_Click");
        }
    }

    private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = e.Uri.AbsoluteUri,
                UseShellExecute = true
            });
            e.Handled = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show("Unable to open the link.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            _ = ErrorLogger.LogAsync(ex, "Error in Hyperlink_RequestNavigate");
        }
    }

    private static string ApplicationVersion
    {
        get
        {
            try
            {
                var version = Assembly.GetExecutingAssembly().GetName().Version;
                return "Version: " + (version?.ToString() ?? "Unknown");
            }
            catch (Exception ex)
            {
                _ = ErrorLogger.LogAsync(ex, "Error getting ApplicationVersion");
                return "Version: Unknown";
            }
        }
    }
}
