using System.Windows.Input;

namespace FindRomCover.Services;

/// <summary>
/// A command that delegates execution and can-execute checks to provided delegates.
/// Implements the <see cref="ICommand"/> interface for use in WPF data binding.
/// </summary>
/// <remarks>
/// This class provides a simple way to create commands without creating separate command classes.
/// It's commonly used in MVVM patterns to bind UI actions to view model methods.
/// </remarks>
public class DelegateCommand : ICommand
{
    private readonly Action<object?> _execute;
    private readonly Func<object?, bool>? _canExecute;

    /// <summary>
    /// Initializes a new instance of the <see cref="DelegateCommand"/> class.
    /// </summary>
    /// <param name="execute">The delegate to execute when the command is invoked. Cannot be null.</param>
    /// <param name="canExecute">
    /// Optional delegate to determine if the command can execute.
    /// If null, the command is always executable.
    /// </param>
    /// <exception cref="ArgumentNullException">Thrown when execute is null.</exception>
    public DelegateCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    /// <summary>
    /// Determines whether the command can execute in its current state.
    /// </summary>
    /// <param name="parameter">Data used by the command. Can be null.</param>
    /// <returns>true if this command can be executed; otherwise, false.</returns>
    public bool CanExecute(object? parameter)
    {
        return _canExecute?.Invoke(parameter) ?? true;
    }

    /// <summary>
    /// Executes the command.
    /// </summary>
    /// <param name="parameter">Data used by the command. Can be null.</param>
    public void Execute(object? parameter)
    {
        _execute(parameter);
    }

    /// <summary>
    /// Occurs when changes occur that affect whether the command should execute.
    /// </summary>
    /// <remarks>
    /// Call <see cref="RaiseCanExecuteChanged"/> to trigger this event and force
    /// WPF to re-query the CanExecute state.
    /// </remarks>
    public event EventHandler? CanExecuteChanged;

    /// <summary>
    /// Raises the <see cref="CanExecuteChanged"/> event to signal that the command's
    /// executable state may have changed.
    /// </summary>
    public void RaiseCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
