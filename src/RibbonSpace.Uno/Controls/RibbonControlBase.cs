using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace RibbonSpace.Controls;

/// <summary>Base class of templated ribbon items (split buttons, inputs, galleries, color pickers...).</summary>
public abstract partial class RibbonControlBase : Control, IRibbonCommandSource
{
    /// <summary>Identifies <see cref="Command"/>.</summary>
    public static readonly DependencyProperty CommandProperty = DependencyProperty.Register(nameof(Command), typeof(ICommand), typeof(RibbonControlBase), new PropertyMetadata(null, (d, e) => ((RibbonControlBase)d).OnCommandChanged((ICommand?)e.OldValue, (ICommand?)e.NewValue)));

    /// <summary>Identifies <see cref="CommandParameter"/>.</summary>
    public static readonly DependencyProperty CommandParameterProperty = DependencyProperty.Register(nameof(CommandParameter), typeof(object), typeof(RibbonControlBase), new PropertyMetadata(null, (d, _) => ((RibbonControlBase)d).UpdateCanExecute()));

    private ICommand? _subscribedCommand;
    private bool _disabledByCommand;
    private bool _settingEnabled;

    /// <summary>Creates the control.</summary>
    protected RibbonControlBase()
    {
        InitializeRibbonItem();
        HorizontalAlignment = HorizontalAlignment.Left;
        VerticalAlignment = VerticalAlignment.Top;

        // CanExecuteChanged is only observed while the item is in the tree, so long-lived commands do not keep
        // discarded items alive.
        Loaded += (_, _) => SubscribeCommand(Command);
        Unloaded += (_, _) => SubscribeCommand(null);
        RegisterPropertyChangedCallback(IsEnabledProperty, (_, _) => OnIsEnabledPropertyChanged());
    }

    /// <summary>Command.</summary>
    public ICommand? Command { get => (ICommand?)GetValue(CommandProperty); set => SetValue(CommandProperty, value); }

    /// <summary>Command parameter.</summary>
    public object? CommandParameter { get => GetValue(CommandParameterProperty); set => SetValue(CommandParameterProperty, value); }

    private void OnCommandChanged(ICommand? oldValue, ICommand? newValue)
    {
        SubscribeCommand(IsLoaded ? newValue : null);
        UpdateCanExecute();
    }

    private void SubscribeCommand(ICommand? command)
    {
        if (ReferenceEquals(_subscribedCommand, command))
        {
            return;
        }

        if (_subscribedCommand is not null)
        {
            _subscribedCommand.CanExecuteChanged -= OnCanExecuteChanged;
        }

        _subscribedCommand = command;
        if (command is not null)
        {
            command.CanExecuteChanged += OnCanExecuteChanged;
            UpdateCanExecute();
        }
    }

    private void OnCanExecuteChanged(object? sender, EventArgs e)
    {
        if (DispatcherQueue is { HasThreadAccess: false } queue)
        {
            queue.TryEnqueue(UpdateCanExecute);
            return;
        }

        UpdateCanExecute();
    }

    /// <summary>
    /// Updates the enabled state from the command. The command only ever disables the item: an item disabled by the
    /// application stays disabled, and the item is re-enabled when CanExecute becomes true again or the command is cleared.
    /// </summary>
    protected virtual void UpdateCanExecute()
    {
        var canExecute = Command is not { } command || !UsesCommandCanExecute || command.CanExecute(CommandParameter);
        if (!canExecute && IsEnabled)
        {
            _disabledByCommand = true;
            SetEnabledFromCommand(false);
        }
        else if (canExecute && _disabledByCommand)
        {
            _disabledByCommand = false;
            SetEnabledFromCommand(true);
        }
    }

    private void SetEnabledFromCommand(bool value)
    {
        _settingEnabled = true;
        try
        {
            IsEnabled = value;
        }
        finally
        {
            _settingEnabled = false;
        }
    }

    private void OnIsEnabledPropertyChanged()
    {
        if (_settingEnabled)
        {
            return;
        }

        if (!IsEnabled)
        {
            // Disabled by the application: re-enabling is up to the application.
            _disabledByCommand = false;
        }
        else if (Command is { } command && UsesCommandCanExecute && !command.CanExecute(CommandParameter))
        {
            // Enabled by the application while the command cannot execute: the command still wins.
            _disabledByCommand = true;
            SetEnabledFromCommand(false);
        }
    }

    /// <summary>When true (default) <see cref="Command"/>.CanExecute drives IsEnabled.</summary>
    protected virtual bool UsesCommandCanExecute => true;

    /// <summary>Executes the command with a parameter (or <see cref="CommandParameter"/>) and notifies the owner.</summary>
    protected bool ExecuteCommand(object? parameter = null)
    {
        var value = parameter ?? CommandParameter;
        var executed = RibbonItemHelper.Execute(this, value);
        if (RibbonItemHelper.GetSourceItem(this) is null)
        {
            RibbonItemHelper.NotifyInvoked(this, value);
        }

        return executed;
    }

    partial void OnLayoutApplied(RibbonItemLayout layout) => OnLayoutAppliedCore(layout);

    /// <summary>Called after <see cref="ApplyLayout"/> changed the presentation.</summary>
    protected virtual void OnLayoutAppliedCore(RibbonItemLayout layout)
    {
    }

    /// <summary>Creates a linked copy (QAT / custom groups).</summary>
    protected virtual FrameworkElement? CreateLinkedCopyCore() => null;

    /// <summary>
    /// Called when this linked copy (QAT, overflow menu, custom group) is discarded: release references to state shared
    /// with the source item (items sources, flyouts, event handlers).
    /// </summary>
    protected virtual void OnUnlinkedCore()
    {
    }

    /// <summary>Creates overflow menu entries.</summary>
    protected virtual IEnumerable<MenuFlyoutItemBase> CreateOverflowMenuItemsCore()
        => [RibbonItemHelper.CreateMenuItem(this, Label, Icon, () => InvokeCore())];

    /// <summary>KeyTip action (focuses the control by default).</summary>
    protected virtual RibbonKeyTipResult OnKeyTipCore()
    {
        Focus(FocusState.Keyboard);
        return RibbonKeyTipResult.Close;
    }

    /// <summary>Primary action.</summary>
    protected virtual bool InvokeCore() => ExecuteCommand();
}
