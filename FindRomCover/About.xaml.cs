using System.Windows;
using System.Diagnostics;
using System.Windows.Navigation;
using System.Reflection;

namespace FindRomCover;

public partial class About
{
    public About()
    {
        InitializeComponent();
            
        App.ApplyThemeToWindow(this);
            
        // Set the data context for data binding
        DataContext = this;
            
        // Set the AppVersionTextBlock 
        AppVersionTextBlock.Text = ApplicationVersion;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = e.Uri.AbsoluteUri,
            UseShellExecute = true
        });
        e.Handled = true;
    }

    private static string ApplicationVersion
    {
        get
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            return "Version: " + (version?.ToString() ?? "Unknown");
        }
    }

}