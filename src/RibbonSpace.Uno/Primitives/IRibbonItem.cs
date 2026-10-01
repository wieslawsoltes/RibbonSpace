using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using RibbonSpace.Commands;
using RibbonSpace.Layout;

namespace RibbonSpace.Controls;

/// <summary>Contract implemented by every ribbon item control (buttons, inputs, galleries...).</summary>
public interface IRibbonItem
{
    /// <summary>Stable id (persistence, QAT, customization).</summary>
    string? Id { get; }

    /// <summary>Label.</summary>
    string? Label { get; }

    /// <summary>Icon.</summary>
    object? Icon { get; }

    /// <summary>Explicit KeyTip.</summary>
    string? KeyTip { get; }

    /// <summary>Command id.</summary>
    string? CommandId { get; }

    /// <summary>Preferred size.</summary>
    RibbonItemSize Size { get; }

    /// <summary>Adaptive size definition.</summary>
    RibbonSizeDefinition EffectiveSizeDefinition { get; }

    /// <summary>Size currently used.</summary>
    RibbonItemSize CurrentSize { get; }

    /// <summary>Simplified ribbon behaviour.</summary>
    RibbonSimplifiedVisibility SimplifiedVisibility { get; }

    /// <summary>Shows the label in the simplified line.</summary>
    bool ShowLabelInSimplifiedMode { get; }

    /// <summary>Whether the item can be added to the QAT.</summary>
    bool CanAddToQuickAccess { get; }

    /// <summary>Applies an adaptive layout (called by groups, toolbars and the QAT).</summary>
    void ApplyLayout(RibbonItemLayout layout);

    /// <summary>Creates a linked copy for the Quick Access Toolbar / custom groups (state stays synchronized).</summary>
    FrameworkElement? CreateLinkedCopy();

    /// <summary>Creates menu entries representing the item in overflow menus.</summary>
    IEnumerable<MenuFlyoutItemBase> CreateOverflowMenuItems();

    /// <summary>Invoked by KeyTips; returns what KeyTip mode should do next.</summary>
    RibbonKeyTipResult OnKeyTip();

    /// <summary>Executes the item's primary action (command search). Returns false when not possible.</summary>
    bool Invoke();
}

/// <summary>Layout request passed to <see cref="IRibbonItem.ApplyLayout"/>.</summary>
/// <param name="Size">Size.</param>
/// <param name="Metrics">Metrics.</param>
/// <param name="IsSimplified">Simplified line / toolbar presentation.</param>
/// <param name="ShowLabel">Label visibility override (QAT / toolbars); <c>null</c> keeps the item default.</param>
/// <param name="GroupState">State of the hosting group (used by containers to size their children).</param>
public readonly record struct RibbonItemLayout(RibbonItemSize Size, RibbonMetrics Metrics, bool IsSimplified = false, bool? ShowLabel = null, RibbonGroupState GroupState = RibbonGroupState.Large);

/// <summary>Result of <see cref="IRibbonItem.OnKeyTip"/>.</summary>
public sealed class RibbonKeyTipResult
{
    /// <summary>Exit KeyTip mode.</summary>
    public static RibbonKeyTipResult Close { get; } = new();

    /// <summary>Continue in a nested scope computed from the given root (e.g. an opened popup).</summary>
    public static RibbonKeyTipResult Scope(Func<IEnumerable<FrameworkElement>> targets) => new() { NextScope = targets };

    /// <summary>Targets of the next KeyTip level.</summary>
    public Func<IEnumerable<FrameworkElement>>? NextScope { get; init; }
}

/// <summary>Owner of ribbon items (ribbon, toolbar, QAT, status bar).</summary>
public interface IRibbonItemOwner
{
    /// <summary>Metrics in effect.</summary>
    RibbonMetrics Metrics { get; }

    /// <summary>Command catalog used to resolve <c>CommandId</c>.</summary>
    RibbonCommandCatalog? CommandCatalog { get; }

    /// <summary>Owning ribbon (null for standalone toolbars).</summary>
    Ribbon? OwnerRibbon { get; }

    /// <summary>Notifies that an item was invoked.</summary>
    void OnItemInvoked(FrameworkElement item, string? commandId, object? parameter);
}

/// <summary>Settable common item properties (implemented by all RibbonSpace item controls; used by the MVVM factory).</summary>
public interface IRibbonItemEditable : IRibbonItem
{
    /// <summary>Id.</summary>
    new string? Id { get; set; }

    /// <summary>Label.</summary>
    new string? Label { get; set; }

    /// <summary>Icon.</summary>
    new object? Icon { get; set; }

    /// <summary>Large icon.</summary>
    object? LargeIcon { get; set; }

    /// <summary>Preferred size.</summary>
    new RibbonItemSize Size { get; set; }

    /// <summary>Size definition text.</summary>
    string? SizeDefinition { get; set; }

    /// <summary>KeyTip.</summary>
    new string? KeyTip { get; set; }

    /// <summary>ScreenTip.</summary>
    object? ScreenTip { get; set; }

    /// <summary>Command id.</summary>
    new string? CommandId { get; set; }

    /// <summary>Shortcut.</summary>
    string? Shortcut { get; set; }

    /// <summary>Simplified behaviour.</summary>
    new RibbonSimplifiedVisibility SimplifiedVisibility { get; set; }

    /// <summary>Simplified label.</summary>
    RibbonSimplifiedLabel SimplifiedLabel { get; set; }

    /// <summary>Label visibility.</summary>
    bool ShowLabel { get; set; }

    /// <summary>QAT availability.</summary>
    new bool CanAddToQuickAccess { get; set; }
}
