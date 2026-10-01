using System.Windows.Input;

namespace RibbonSpace.Commands;

/// <summary>Lightweight <see cref="ICommand"/> implementation delegating to callbacks.</summary>
public class RibbonRelayCommand : ICommand
{
    private readonly Action<object?> _execute;
    private readonly Func<object?, bool>? _canExecute;

    /// <summary>Creates a command ignoring its parameter.</summary>
    public RibbonRelayCommand(Action execute, Func<bool>? canExecute = null)
        : this(_ => execute(), canExecute is null ? null : _ => canExecute())
    {
        ArgumentNullException.ThrowIfNull(execute);
    }

    /// <summary>Creates a command receiving its parameter.</summary>
    public RibbonRelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    /// <inheritdoc />
    public event EventHandler? CanExecuteChanged;

    /// <inheritdoc />
    public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;

    /// <inheritdoc />
    public void Execute(object? parameter)
    {
        if (CanExecute(parameter))
        {
            _execute(parameter);
        }
    }

    /// <summary>Raises <see cref="CanExecuteChanged"/>.</summary>
    public void NotifyCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}

/// <summary>Strongly typed <see cref="RibbonRelayCommand"/>.</summary>
public sealed class RibbonRelayCommand<T> : RibbonRelayCommand
{
    /// <summary>Creates a typed command. Parameters of another type are passed as <c>default</c>.</summary>
    public RibbonRelayCommand(Action<T?> execute, Func<T?, bool>? canExecute = null)
        : base(p => execute(p is T t ? t : default), canExecute is null ? null : p => canExecute(p is T t ? t : default))
    {
    }
}

/// <summary>Asynchronous command that disables itself while running and reports failures.</summary>
public sealed class RibbonAsyncCommand : ICommand
{
    private readonly Func<object?, Task> _execute;
    private readonly Func<object?, bool>? _canExecute;
    private bool _isRunning;

    /// <summary>Creates an asynchronous command.</summary>
    public RibbonAsyncCommand(Func<object?, Task> execute, Func<object?, bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    /// <summary>Creates an asynchronous command ignoring its parameter.</summary>
    public RibbonAsyncCommand(Func<Task> execute, Func<bool>? canExecute = null)
        : this(_ => execute(), canExecute is null ? null : _ => canExecute())
    {
    }

    /// <summary>Raised when the command throws. When not handled the exception is rethrown.</summary>
    public event EventHandler<RibbonCommandErrorEventArgs>? Failed;

    /// <inheritdoc />
    public event EventHandler? CanExecuteChanged;

    /// <summary>True while the command runs.</summary>
    public bool IsRunning => _isRunning;

    /// <inheritdoc />
    public bool CanExecute(object? parameter) => !_isRunning && (_canExecute?.Invoke(parameter) ?? true);

    /// <inheritdoc />
    public async void Execute(object? parameter) => await ExecuteAsync(parameter);

    /// <summary>Executes the command and awaits completion.</summary>
    public async Task ExecuteAsync(object? parameter)
    {
        if (!CanExecute(parameter))
        {
            return;
        }

        _isRunning = true;
        NotifyCanExecuteChanged();
        try
        {
            await _execute(parameter).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            var args = new RibbonCommandErrorEventArgs(ex);
            Failed?.Invoke(this, args);
            if (!args.Handled)
            {
                throw;
            }
        }
        finally
        {
            _isRunning = false;
            NotifyCanExecuteChanged();
        }
    }

    /// <summary>Raises <see cref="CanExecuteChanged"/>.</summary>
    public void NotifyCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}

/// <summary>Arguments of <see cref="RibbonAsyncCommand.Failed"/>.</summary>
public sealed class RibbonCommandErrorEventArgs(Exception exception) : EventArgs
{
    /// <summary>The exception thrown by the command.</summary>
    public Exception Exception { get; } = exception;

    /// <summary>Set to true to swallow the exception.</summary>
    public bool Handled { get; set; }
}
