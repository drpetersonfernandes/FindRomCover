using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Text;
using System.Windows;
using FindRomCover.Models;
using FindRomCover.Services;

namespace FindRomCover;

public partial class DebugWindow
{
    private bool _isForceClosing;
    private readonly ObservableCollection<LogEntry> _logMessages;

    public DebugWindow()
    {
        InitializeComponent();

        _logMessages = LogService.GetLogMessages();

        // Populate the TextBox with existing logs on startup
        var initialLogText = new StringBuilder();
        foreach (var logEntry in _logMessages)
        {
            initialLogText.AppendLine(logEntry.Message);
        }

        LogTextBox.Text = initialLogText.ToString();
        LogTextBox.ScrollToEnd();

        // Subscribe to collection changes to append new logs
        if (_logMessages is INotifyCollectionChanged incc)
        {
            incc.CollectionChanged += LogMessages_CollectionChanged;
        }

        Closing += OnLogWindowClosing;
        Closed += OnLogWindowClosed;
        LogService.Information("Log window initialized.");
    }

    public void ForceClose()
    {
        _isForceClosing = true;
    }

    private void OnLogWindowClosing(object? sender, CancelEventArgs e)
    {
        if (_isForceClosing)
        {
            // Allow the window to close if the application is shutting down.
            return;
        }

        // Instead of closing, just hide the window. The main window can re-show it.
        e.Cancel = true;
        Hide();
        LogService.Information("Log window hidden.");
    }

    private void OnLogWindowClosed(object? sender, EventArgs e)
    {
        // Unsubscribe from CollectionChanged to prevent memory leak
        if (_logMessages is INotifyCollectionChanged incc)
        {
            incc.CollectionChanged -= LogMessages_CollectionChanged;
        }

        LogService.Information("Log window closed and event handlers unsubscribed.");
    }

    private void LogMessages_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            // When a new log is added to the source collection...
            case NotifyCollectionChangedAction.Add when e.NewItems is not null:
                {
                    var newText = new StringBuilder();
                    foreach (LogEntry item in e.NewItems)
                    {
                        newText.AppendLine(item.Message);
                    }

                    // ...append it to our TextBox.
                    LogTextBox.AppendText(newText.ToString());
                    LogTextBox.ScrollToEnd();
                    break;
                }
            // When the source collection is cleared...
            case NotifyCollectionChangedAction.Reset:
                // ...clear our TextBox.
                LogTextBox.Clear();
                break;
        }
    }

    private void CopyLog_Click(object sender, RoutedEventArgs e)
    {
        LogService.Information("Copy All button clicked.");
        try
        {
            if (LogTextBox.Text.Length > 0)
            {
                Clipboard.SetText(LogTextBox.Text);
                LogService.Information("Entire log content copied to clipboard.");
            }
            else
            {
                LogService.Information("Log is empty, nothing to copy.");
            }
        }
        catch (Exception ex)
        {
            LogService.Error(ex, "Failed to copy log to clipboard.");
        }
    }

    private void ClearLog_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            LogService.Information("Clear button clicked. Clearing log messages from view.");
            // This will trigger the CollectionChanged event with a 'Reset' action,
            // which will then clear the TextBox.
            _logMessages.Clear();
        }
        catch (Exception ex) { LogService.Error(ex, "Error in ClearLog_Click"); }
    }

    private void CopySelection_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(LogTextBox.SelectedText)) return;

        LogService.Information("Copy Selection clicked.");
        try
        {
            Clipboard.SetText(LogTextBox.SelectedText);
            LogService.Information("Selected log text copied to clipboard.");
        }
        catch (Exception ex)
        {
            LogService.Error(ex, "Failed to copy selection to clipboard.");
        }
    }

    private void LogTextBox_SelectionChanged(object sender, RoutedEventArgs e)
    {
        try
        {
            var hasSelection = !string.IsNullOrEmpty(LogTextBox.SelectedText);
            CopySelectionButton.IsEnabled = hasSelection;
            ContextMenuCopySelection.IsEnabled = hasSelection;
        }
        catch (Exception ex) { LogService.Error(ex, "Error in LogTextBox_SelectionChanged"); }
    }
}
