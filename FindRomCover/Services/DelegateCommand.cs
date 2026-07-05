using System.Windows.Input;

namespace FindRomCover.Services;

public class DelegateCommand : ICommand, IDisposable
{
    private readonly Action<object?> _execute;
    private readonly Func<object?, bool>? _canExecute;
    private bool _disposed;

    public DelegateCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
        CommandManager.RequerySuggested += OnRequerySuggested;
    }

    private void OnRequerySuggested(object? sender, EventArgs e)
    {
        CanExecuteChanged?.Invoke(this, e);
    }

    public bool CanExecute(object? parameter)
    {
        return _canExecute?.Invoke(parameter) ?? true;
    }

    public void Execute(object? parameter)
    {
        _execute(parameter);
    }

    public event EventHandler? CanExecuteChanged;

    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;
        CommandManager.RequerySuggested -= OnRequerySuggested;
    }
}
