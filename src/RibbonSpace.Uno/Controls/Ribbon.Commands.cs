using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using RibbonSpace.Commands;

namespace RibbonSpace.Controls;

public partial class Ribbon
{
    private readonly Dictionary<string, (bool? Enabled, bool? Checked)> _commandStates = new(StringComparer.OrdinalIgnoreCase);
    private RibbonCommandCatalog? _subscribedCatalog;

    /// <summary>Enables or disables every item bound to a command id (persisted for items created later).</summary>
    public void SetCommandEnabled(string commandId, bool enabled)
    {
        _commandStates[commandId] = (enabled, _commandStates.GetValueOrDefault(commandId).Checked);
        foreach (var item in FindItemsByCommand(commandId))
        {
            ApplyCommandState(item);
        }
    }

    /// <summary>Checks or unchecks every toggle item bound to a command id.</summary>
    public void SetCommandChecked(string commandId, bool isChecked)
    {
        _commandStates[commandId] = (_commandStates.GetValueOrDefault(commandId).Enabled, isChecked);
        foreach (var item in FindItemsByCommand(commandId))
        {
            ApplyCommandState(item);
        }
    }

    /// <summary>Items (including QAT copies) bound to a command id.</summary>
    public IEnumerable<FrameworkElement> FindItemsByCommand(string commandId)
        => GetAllItems().Concat(QuickAccessToolBar?.Items.OfType<FrameworkElement>() ?? [])
            .Where(i => i is IRibbonItem { CommandId: { } id } && string.Equals(id, commandId, StringComparison.OrdinalIgnoreCase));

    internal void ApplyCommandState(FrameworkElement item)
    {
        if (item is not IRibbonItem { CommandId: { } id } || !_commandStates.TryGetValue(id, out var state))
        {
            return;
        }

        if (state.Enabled is { } enabled && item is Control control)
        {
            control.IsEnabled = enabled;
        }

        if (state.Checked is { } isChecked && item is IRibbonCheckable checkable)
        {
            checkable.IsChecked = isChecked;
        }
    }

    private void OnCommandCatalogChanged()
    {
        DetachCommandCatalog();
        if (IsLoaded)
        {
            AttachCommandCatalog();
        }

        foreach (var item in GetAllItems())
        {
            RibbonItemHelper.ResolveCommand(item);
        }
    }

    private void AttachCommandCatalog()
    {
        if (ReferenceEquals(_subscribedCatalog, CommandCatalog))
        {
            return;
        }

        DetachCommandCatalog();
        _subscribedCatalog = CommandCatalog;
        if (_subscribedCatalog is not null)
        {
            _subscribedCatalog.CommandsChanged += OnCatalogCommandsChanged;
        }
    }

    private void DetachCommandCatalog()
    {
        if (_subscribedCatalog is not null)
        {
            _subscribedCatalog.CommandsChanged -= OnCatalogCommandsChanged;
            _subscribedCatalog = null;
        }
    }

    private void OnCatalogCommandsChanged(object? sender, string id)
    {
        DispatcherQueue?.TryEnqueue(() =>
        {
            foreach (var item in FindItemsByCommand(id))
            {
                RibbonItemHelper.ResolveCommand(item);
            }
        });
    }
}
