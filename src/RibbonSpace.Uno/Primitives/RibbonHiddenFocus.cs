using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace RibbonSpace.Controls.Primitives;

/// <summary>
/// Removes elements that a panel arranges out of view (0x0: scrolled gallery rows, overflowed toolbar items, items moved
/// to the simplified overflow menu, hidden groups) from keyboard tab navigation, and restores them when shown again.
/// </summary>
internal static class RibbonHiddenFocus
{
    // Original local IsTabStop value (or UnsetValue) of every control currently hidden from focus.
    private static readonly ConditionalWeakTable<Control, object> Hidden = new();

    /// <summary>Hides an element (and the controls inside it) from tab navigation, or restores it.</summary>
    public static void Set(UIElement element, bool hidden)
    {
        // Fast path (panels call this on every arrange): the control already is in the requested state.
        if (element is Control control && Hidden.TryGetValue(control, out _) == hidden)
        {
            return;
        }

        // An element shown by its own panel stays hidden while an enclosing panel hides one of its ancestors.
        if (!hidden && HasHiddenAncestor(element))
        {
            return;
        }

        Apply(element, hidden);
    }

    private static bool HasHiddenAncestor(DependencyObject element)
    {
        var current = VisualTreeHelper.GetParent(element);
        while (current is not null)
        {
            if (current is Control control && Hidden.TryGetValue(control, out _))
            {
                return true;
            }

            current = VisualTreeHelper.GetParent(current);
        }

        return false;
    }

    private static void Apply(UIElement element, bool hidden)
    {
        if (element is Control control)
        {
            var isHidden = Hidden.TryGetValue(control, out var original);
            if (isHidden == hidden)
            {
                return;
            }

            if (hidden)
            {
                Hidden.Add(control, control.ReadLocalValue(Control.IsTabStopProperty));
                control.IsTabStop = false;
            }
            else
            {
                Hidden.Remove(control);
                if (original == DependencyProperty.UnsetValue)
                {
                    control.ClearValue(Control.IsTabStopProperty);
                }
                else
                {
                    control.SetValue(Control.IsTabStopProperty, original);
                }
            }
        }

        var count = VisualTreeHelper.GetChildrenCount(element);
        for (var i = 0; i < count; i++)
        {
            if (VisualTreeHelper.GetChild(element, i) is UIElement child)
            {
                Apply(child, hidden);
            }
        }
    }
}
