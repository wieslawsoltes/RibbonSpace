using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;
using Windows.Foundation;

namespace RibbonSpace.Controls.Primitives;

/// <summary>Places ribbon popups next to an anchor and keeps them inside the window.</summary>
public static class RibbonPopupPlacement
{
    /// <summary>
    /// Positions <paramref name="popup"/> below (or, when there is no room, above) <paramref name="anchor"/>,
    /// clamped horizontally to the window.
    /// </summary>
    /// <param name="popup">Popup (its Child is measured).</param>
    /// <param name="anchor">Anchor element.</param>
    /// <param name="overlapAnchor">True to align the popup's top with the anchor's top (expanded galleries).</param>
    /// <param name="gap">Vertical gap.</param>
    public static void PlaceBelow(Popup popup, FrameworkElement anchor, bool overlapAnchor = false, double gap = 2)
    {
        ArgumentNullException.ThrowIfNull(popup);
        ArgumentNullException.ThrowIfNull(anchor);
        var root = anchor.XamlRoot;
        popup.XamlRoot = root;
        Point origin;
        try
        {
            origin = anchor.TransformToVisual(null).TransformPoint(new Point(0, 0));
        }
        catch (ArgumentException)
        {
            origin = new Point(0, 0);
        }

        var size = new Size(0, 0);
        if (popup.Child is FrameworkElement child)
        {
            child.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            size = child.DesiredSize;
        }

        var windowWidth = root?.Size.Width ?? double.PositiveInfinity;
        var windowHeight = root?.Size.Height ?? double.PositiveInfinity;
        var x = origin.X;
        if (x + size.Width > windowWidth - 4)
        {
            x = Math.Max(4, windowWidth - size.Width - 4);
        }

        var y = overlapAnchor ? origin.Y : origin.Y + anchor.ActualHeight + gap;
        if (y + size.Height > windowHeight - 4 && origin.Y - size.Height - gap > 4 && !overlapAnchor)
        {
            y = origin.Y - size.Height - gap;
        }
        else if (y + size.Height > windowHeight - 4)
        {
            y = Math.Max(4, windowHeight - size.Height - 4);
        }

        popup.HorizontalOffset = x;
        popup.VerticalOffset = y;
    }
}
