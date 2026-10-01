using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using RibbonSpace.Controls.Primitives;
using RibbonSpace.Localization;
using Windows.Foundation;

namespace RibbonSpace.Controls;

/// <summary>How groups are presented when the ribbon is minimized to panel buttons or panel titles (AutoCAD).</summary>
public enum RibbonPanelPresentation
{
    /// <summary>Full panels (normal ribbon).</summary>
    Full,
    /// <summary>One button per panel (icon and title); clicking opens the panel.</summary>
    Buttons,
    /// <summary>Panel titles only; clicking a title opens the panel.</summary>
    Titles,
}

// AutoCAD-class panel features of RibbonGroup: expanded (slide-out) panels with a pushpin, panel buttons / titles
// presentation and floating panels.
public partial class RibbonGroup
{
    /// <summary>Identifies <see cref="IsFloating"/>.</summary>
    public static readonly DependencyProperty IsFloatingProperty = DependencyProperty.Register(nameof(IsFloating), typeof(bool), typeof(RibbonGroup), new PropertyMetadata(false, (d, e) => ((RibbonGroup)d).OnIsFloatingChanged((bool)e.NewValue)));

    /// <summary>Identifies <see cref="IsSlideOutPinned"/>.</summary>
    public static readonly DependencyProperty IsSlideOutPinnedProperty = DependencyProperty.Register(nameof(IsSlideOutPinned), typeof(bool), typeof(RibbonGroup), new PropertyMetadata(false, (d, _) => ((RibbonGroup)d).OnSlideOutPinnedChanged()));

    private readonly StackPanel _slideOutPanel = new() { Spacing = 1 };
    private Button? _slideOutButton;
    private Popup? _slideOutPopup;
    private Border? _slideOutHost;
    private ToggleButton? _pinButton;
    private TextBlock? _slideOutHeader;
    private Border? _popupSlideOutHost;
    private Popup? _floatPopup;
    private Border? _floatItemsHost;
    private Border? _floatSlideOutHost;
    private TextBlock? _floatHeader;
    private RibbonPanelPresentation _panelPresentation;
    private Point? _captionDragStart;
    private Point _floatDragStart;
    private bool _floatDragging;

    /// <summary>Raised when the group starts or stops floating.</summary>
    public event EventHandler? FloatingChanged;

    /// <summary>
    /// Less frequently used commands shown in the expanded part of the panel, opened from the arrow in the panel title
    /// (AutoCAD expanded panels). The pushpin keeps it open (<see cref="IsSlideOutPinned"/>).
    /// </summary>
    public ObservableCollection<UIElement> SlideOutItems { get; } = [];

    /// <summary>Keeps the expanded panel open (pushpin) until unpinned; it reopens when the tab is selected again.</summary>
    public bool IsSlideOutPinned { get => (bool)GetValue(IsSlideOutPinnedProperty); set => SetValue(IsSlideOutPinnedProperty, value); }

    /// <summary>True while the expanded panel is shown.</summary>
    public bool IsSlideOutOpen => _slideOutPopup?.IsOpen == true;

    /// <summary>The arrow in the panel title that opens the expanded panel (KeyTips, tests).</summary>
    public Button? SlideOutButton => _slideOutButton;

    /// <summary>
    /// The panel floats in its own window-level panel outside the ribbon (AutoCAD floating panels); it stays open when
    /// other tabs are selected until returned to the ribbon.
    /// </summary>
    public bool IsFloating { get => (bool)GetValue(IsFloatingProperty); set => SetValue(IsFloatingProperty, value); }

    /// <summary>Position of the floating panel in window coordinates.</summary>
    public Point FloatingPosition { get; set; } = new(120, 160);

    /// <summary>True while the floating panel is on screen.</summary>
    public bool IsFloatingPanelOpen => _floatPopup?.IsOpen == true;

    /// <summary>Presentation forced by the ribbon (panel buttons / titles minimize states).</summary>
    public RibbonPanelPresentation PanelPresentation
    {
        get => _panelPresentation;
        internal set
        {
            if (_panelPresentation == value)
            {
                return;
            }

            _panelPresentation = value;
            _state = null;
            OnHeaderChanged();
            ApplyCollapsedButtonLayout();
        }
    }

    private bool ShowsCaption => Ribbon?.ShowGroupCaptions ?? true;

    /// <summary>Forces the next layout pass to re-apply the presentation (captions toggled, minimize state changed).</summary>
    internal void ResetPresentationState() => _state = null;

    private void AttachPanelParts()
    {
        _slideOutButton = GetTemplateChild("PART_SlideOutButton") as Button;
        if (_slideOutButton is not null)
        {
            _slideOutButton.Click += OnSlideOutButtonClick;
        }

        if (_captionRow is not null)
        {
            _captionRow.PointerPressed += OnCaptionPointerPressed;
            _captionRow.PointerMoved += OnCaptionPointerMoved;
            _captionRow.PointerReleased += OnCaptionPointerReleased;
            _captionRow.PointerCaptureLost += OnCaptionPointerReleased;
        }

        UpdateSlideOutButton();
    }

    private void DetachPanelParts()
    {
        if (_slideOutButton is not null)
        {
            _slideOutButton.Click -= OnSlideOutButtonClick;
        }

        if (_captionRow is not null)
        {
            _captionRow.PointerPressed -= OnCaptionPointerPressed;
            _captionRow.PointerMoved -= OnCaptionPointerMoved;
            _captionRow.PointerReleased -= OnCaptionPointerReleased;
            _captionRow.PointerCaptureLost -= OnCaptionPointerReleased;
        }
    }

    private void ApplyCollapsedButtonLayout()
        => _collapsedButton?.ApplyLayout(PanelPresentation == RibbonPanelPresentation.Titles
            ? new RibbonItemLayout(RibbonItemSize.Medium, _metrics, false, true)
            : new RibbonItemLayout(RibbonItemSize.Large, _metrics));

    // ---------------------------------------------------------------- expanded (slide-out) panel

    private void OnSlideOutItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        _slideOutPanel.Children.Clear();
        foreach (var item in SlideOutItems)
        {
            if (item is FrameworkElement { Parent: Panel parent } && !ReferenceEquals(parent, _slideOutPanel))
            {
                parent.Children.Remove(item);
            }

            if (Ribbon is { } ribbon)
            {
                RibbonItemHelper.SetOwner(item, ribbon);
            }

            _slideOutPanel.Children.Add(item);
        }

        ApplySlideOutLayouts();
        UpdateSlideOutButton();
        Ribbon?.InvalidateShortcuts();
        InvalidateItemsLayout();
    }

    private void ApplySlideOutLayouts()
    {
        // Expanded panels list commands with labels: Large items become Medium, smaller ones keep their size.
        foreach (var item in SlideOutItems.OfType<IRibbonItem>())
        {
            var preferred = item.EffectiveSizeDefinition.GetSize(RibbonGroupState.Large);
            var size = preferred == RibbonItemSize.Large ? RibbonItemSize.Medium : preferred;
            item.ApplyLayout(new RibbonItemLayout(size, _metrics));
        }
    }

    private void UpdateSlideOutButton()
    {
        if (_slideOutButton is null)
        {
            return;
        }

        var visible = SlideOutItems.Any(i => i.Visibility == Visibility.Visible) && !IsSimplified && State != RibbonGroupState.Collapsed;
        _slideOutButton.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        _slideOutButton.Content = IsSlideOutOpen ? "" : "";
        var name = RibbonStrings.Current.Format(nameof(RibbonStrings.ExpandPanel), Header ?? string.Empty);
        AutomationProperties.SetName(_slideOutButton, name);
        ToolTipService.SetToolTip(_slideOutButton, name);
    }

    private void OnSlideOutButtonClick(object sender, RoutedEventArgs e)
    {
        if (IsSlideOutOpen)
        {
            IsSlideOutPinned = false;
            CloseSlideOut();
        }
        else
        {
            // Deferred: the click must not reach the new popup's light-dismiss layer.
            DispatcherQueue?.TryEnqueue(OpenSlideOut);
        }
    }

    /// <summary>Opens the expanded part of the panel below the group.</summary>
    public void OpenSlideOut()
    {
        if (SlideOutItems.Count == 0 || XamlRoot is null || !IsLoaded || IsPopupOpen || IsFloating || Visibility != Visibility.Visible)
        {
            return;
        }

        if (_slideOutPopup is null)
        {
            _slideOutHost = new Border { Padding = new Thickness(4, 3, 4, 3) };
            _pinButton = new ToggleButton
            {
                Content = new FontIcon { Glyph = "", FontSize = 11 },
                Padding = new Thickness(0),
                Width = 22,
                Height = 18,
                MinWidth = 0,
                MinHeight = 0,
                BorderThickness = new Thickness(0),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
            };
            _pinButton.Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent);
            RibbonTheme.SetThemeBrush(_pinButton, Control.ForegroundProperty, "RibbonSecondaryForegroundBrush");
            _pinButton.Click += (_, _) => IsSlideOutPinned = _pinButton.IsChecked == true;
            _slideOutHeader = new TextBlock { FontSize = _metrics.CaptionFontSize, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(24, 0, 24, 1) };
            RibbonTheme.SetThemeBrush(_slideOutHeader, TextBlock.ForegroundProperty, "RibbonGroupCaptionForegroundBrush");
            var bar = new Grid { Height = _metrics.GroupCaptionHeight + 2, Children = { _slideOutHeader, _pinButton } };
            RibbonTheme.SetThemeBrush(bar, Panel.BackgroundProperty, "RibbonGroupCaptionBackgroundBrush");
            var layout = new Grid { RowDefinitions = { new RowDefinition { Height = GridLength.Auto }, new RowDefinition { Height = GridLength.Auto } } };
            layout.Children.Add(_slideOutHost);
            Grid.SetRow(bar, 1);
            layout.Children.Add(bar);
            var chrome = new Border { Child = layout, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(0, 0, 3, 3) };
            RibbonTheme.SetThemeBrush(chrome, Border.BackgroundProperty, "RibbonCommandBarBackgroundBrush");
            RibbonTheme.SetThemeBrush(chrome, Border.BorderBrushProperty, "RibbonPopupBorderBrush");
            chrome.KeyDown += (_, e) =>
            {
                if (e.Key == Windows.System.VirtualKey.Escape && !e.Handled)
                {
                    IsSlideOutPinned = false;
                    CloseSlideOut();
                    _slideOutButton?.Focus(FocusState.Keyboard);
                    e.Handled = true;
                }
            };
            _slideOutPopup = new Popup { Child = chrome };
            RibbonItemHelper.SetOwner(chrome, Ribbon);
            _slideOutPopup.Closed += (_, _) =>
            {
                if (_slideOutHost is not null && ReferenceEquals(_slideOutHost.Child, _slideOutPanel))
                {
                    _slideOutHost.Child = null;
                }

                UpdateSlideOutButton();
            };
        }

        DetachSlideOutPanel();
        _slideOutHost!.Child = _slideOutPanel;
        ApplySlideOutLayouts();
        _slideOutHeader!.Text = Header ?? string.Empty;
        _pinButton!.IsChecked = IsSlideOutPinned;
        UpdatePinButton();
        _slideOutPopup.IsLightDismissEnabled = !IsSlideOutPinned;
        _slideOutPopup.XamlRoot = XamlRoot;
        if (_slideOutPopup.Child is FrameworkElement child)
        {
            child.RequestedTheme = ActualTheme;
            child.MinWidth = ActualWidth;
        }

        Ribbon?.AttachPopupKeyboard(_slideOutPopup.Child);
        RibbonPopupPlacement.PlaceBelow(_slideOutPopup, this);
        _slideOutPopup.IsOpen = true;
        UpdateSlideOutButton();
    }

    /// <summary>Closes the expanded part of the panel (keeps <see cref="IsSlideOutPinned"/>).</summary>
    public void CloseSlideOut()
    {
        if (_slideOutPopup is { IsOpen: true })
        {
            _slideOutPopup.IsOpen = false;
        }
    }

    private void OnSlideOutPinnedChanged()
    {
        if (_slideOutPopup is not null)
        {
            _slideOutPopup.IsLightDismissEnabled = !IsSlideOutPinned;
        }

        if (_pinButton is not null)
        {
            _pinButton.IsChecked = IsSlideOutPinned;
        }

        UpdatePinButton();
        Ribbon?.RaiseStateChanged();
    }

    private void UpdatePinButton()
    {
        if (_pinButton is null)
        {
            return;
        }

        var text = IsSlideOutPinned ? RibbonStrings.Current.UnpinPanel : RibbonStrings.Current.PinPanel;
        AutomationProperties.SetName(_pinButton, text);
        ToolTipService.SetToolTip(_pinButton, text);
        if (_pinButton.Content is FontIcon icon)
        {
            icon.Glyph = IsSlideOutPinned ? "" : "";
        }
    }

    private void DetachSlideOutPanel()
    {
        if (_slideOutPanel.Parent is Border border)
        {
            border.Child = null;
        }
    }

    // The popup of a collapsed group (and panel buttons / titles) shows the expanded part below the main items.
    private void MoveSlideOutIntoPopup()
    {
        if (_popupSlideOutHost is null)
        {
            return;
        }

        if (SlideOutItems.Count == 0)
        {
            _popupSlideOutHost.Child = null;
            _popupSlideOutHost.BorderThickness = new Thickness(0);
            return;
        }

        CloseSlideOut();
        DetachSlideOutPanel();
        ApplySlideOutLayouts();
        _popupSlideOutHost.BorderThickness = new Thickness(0, 1, 0, 0);
        _popupSlideOutHost.Padding = new Thickness(0, 3, 0, 0);
        RibbonTheme.SetThemeBrush(_popupSlideOutHost, Border.BorderBrushProperty, "RibbonSeparatorBrush");
        _popupSlideOutHost.Child = _slideOutPanel;
    }

    private void MoveSlideOutOutOfPopup()
    {
        if (_popupSlideOutHost is not null && ReferenceEquals(_popupSlideOutHost.Child, _slideOutPanel))
        {
            _popupSlideOutHost.Child = null;
        }
    }

    /// <summary>KeyTip targets of the expanded part.</summary>
    internal IEnumerable<FrameworkElement> GetSlideOutKeyTipTargets()
        => SlideOutItems.Where(i => i.Visibility == Visibility.Visible).SelectMany(Flatten);

    // ---------------------------------------------------------------- floating panels

    /// <summary>Floats the panel at a window position (defaults to just below its place in the ribbon).</summary>
    public void Float(Point? position = null)
    {
        if (position is { } p)
        {
            FloatingPosition = p;
        }
        else if (XamlRoot is not null && IsLoaded)
        {
            var origin = TransformToVisual(null).TransformPoint(new Point(0, ActualHeight + 8));
            FloatingPosition = origin;
        }

        IsFloating = true;
    }

    /// <summary>Returns a floating panel to its place in the ribbon.</summary>
    public void ReturnToRibbon() => IsFloating = false;

    private void OnIsFloatingChanged(bool floating)
    {
        ClosePopup();
        CloseSlideOut();
        if (floating)
        {
            ShowFloat();
        }
        else
        {
            HideFloat();
        }

        UpdateVisibility();
        FloatingChanged?.Invoke(this, EventArgs.Empty);
        Ribbon?.OnGroupFloatingChanged(this);
    }

    /// <summary>Shows the floating panel (when the ribbon is in the tree).</summary>
    internal void ShowFloat()
    {
        var root = Ribbon?.XamlRoot ?? XamlRoot;
        if (!IsFloating || root is null)
        {
            return;
        }

        if (_floatPopup is null)
        {
            BuildFloatingPanel();
        }

        if (_itemsPresenter is not null && ReferenceEquals(_itemsPresenter.Child, _itemsPanel))
        {
            _itemsPresenter.Child = null;
        }

        if (_itemsPanel.Parent is Border previous && !ReferenceEquals(previous, _floatItemsHost))
        {
            previous.Child = null;
        }

        _state = null;
        _itemsPanel.IsSimplified = false;
        ApplyItemLayouts(RibbonGroupState.Large, false, _metrics);
        _floatItemsHost!.Child = _itemsPanel;
        if (SlideOutItems.Count > 0)
        {
            DetachSlideOutPanel();
            ApplySlideOutLayouts();
            _floatSlideOutHost!.Child = _slideOutPanel;
            _floatSlideOutHost.Visibility = Visibility.Visible;
        }
        else
        {
            _floatSlideOutHost!.Visibility = Visibility.Collapsed;
        }

        _floatHeader!.Text = Header ?? string.Empty;
        _floatPopup!.XamlRoot = root;
        if (_floatPopup.Child is FrameworkElement chrome)
        {
            chrome.RequestedTheme = Ribbon?.ActualTheme ?? ActualTheme;
        }

        ClampFloatingPosition(root);
        _floatPopup.HorizontalOffset = FloatingPosition.X;
        _floatPopup.VerticalOffset = FloatingPosition.Y;
        Ribbon?.AttachPopupKeyboard(_floatPopup.Child);
        _floatPopup.IsOpen = true;
    }

    /// <summary>Hides the floating panel without returning it (ribbon unloaded).</summary>
    internal void SuspendFloat()
    {
        if (_floatPopup is { IsOpen: true })
        {
            _floatPopup.IsOpen = false;
        }
    }

    private void HideFloat()
    {
        SuspendFloat();
        if (_floatItemsHost is not null && ReferenceEquals(_floatItemsHost.Child, _itemsPanel))
        {
            _floatItemsHost.Child = null;
        }

        if (_floatSlideOutHost is not null && ReferenceEquals(_floatSlideOutHost.Child, _slideOutPanel))
        {
            _floatSlideOutHost.Child = null;
        }

        if (_itemsPresenter is not null)
        {
            _itemsPresenter.Child = _itemsPanel;
        }

        _state = null;
        InvalidateItemsLayout();
        Tab?.InvalidateGroupsLayout();
    }

    private void BuildFloatingPanel()
    {
        var strings = RibbonStrings.Current;
        var returnButton = new Button
        {
            Content = new FontIcon { Glyph = "", FontSize = 10 },
            Padding = new Thickness(0),
            Width = 18,
            Height = 20,
            MinWidth = 0,
            MinHeight = 0,
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        if (Application.Current.Resources.TryGetValue("RibbonChromeButtonStyle", out var chromeStyle) && chromeStyle is Style style)
        {
            returnButton.Style = style;
        }

        AutomationProperties.SetName(returnButton, strings.ReturnPanelToRibbon);
        ToolTipService.SetToolTip(returnButton, strings.ReturnPanelToRibbon);
        returnButton.Click += (_, _) => ReturnToRibbon();
        var grip = new TextBlock { Text = "⋮⋮", FontSize = 10, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, IsHitTestVisible = false };
        RibbonTheme.SetThemeBrush(grip, TextBlock.ForegroundProperty, "RibbonSecondaryForegroundBrush");
        var bar = new Grid { Width = 20, RowDefinitions = { new RowDefinition { Height = GridLength.Auto }, new RowDefinition { Height = new GridLength(1, GridUnitType.Star) } } };
        bar.Children.Add(returnButton);
        Grid.SetRow(grip, 1);
        bar.Children.Add(grip);
        RibbonTheme.SetThemeBrush(bar, Panel.BackgroundProperty, "RibbonFloatingPanelBarBrush");
        AttachFloatDrag(bar);

        _floatItemsHost = new Border { Padding = new Thickness(4, 3, 4, 0) };
        _floatSlideOutHost = new Border { BorderThickness = new Thickness(0, 1, 0, 0), Padding = new Thickness(4, 3, 4, 0) };
        RibbonTheme.SetThemeBrush(_floatSlideOutHost, Border.BorderBrushProperty, "RibbonSeparatorBrush");
        _floatHeader = new TextBlock { FontSize = _metrics.CaptionFontSize, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0, 8, 1) };
        RibbonTheme.SetThemeBrush(_floatHeader, TextBlock.ForegroundProperty, "RibbonGroupCaptionForegroundBrush");
        var caption = new Grid { Height = _metrics.GroupCaptionHeight + 2, Children = { _floatHeader } };
        RibbonTheme.SetThemeBrush(caption, Panel.BackgroundProperty, "RibbonGroupCaptionBackgroundBrush");
        AttachFloatDrag(caption);
        var content = new StackPanel { Children = { _floatItemsHost, _floatSlideOutHost, caption } };
        var layout = new Grid { ColumnDefinitions = { new ColumnDefinition { Width = GridLength.Auto }, new ColumnDefinition { Width = GridLength.Auto } } };
        layout.Children.Add(bar);
        Grid.SetColumn(content, 1);
        layout.Children.Add(content);
        var chrome = new Border { Child = layout, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(3) };
        RibbonTheme.SetThemeBrush(chrome, Border.BackgroundProperty, "RibbonCommandBarBackgroundBrush");
        RibbonTheme.SetThemeBrush(chrome, Border.BorderBrushProperty, "RibbonPopupBorderBrush");
        RibbonItemHelper.SetOwner(chrome, Ribbon);
        AutomationProperties.SetName(chrome, Header ?? string.Empty);
        chrome.RightTapped += (_, e) =>
        {
            if (!e.Handled && Ribbon is { } ribbon)
            {
                ribbon.ShowItemContextMenu(this, e.GetPosition(this));
                e.Handled = true;
            }
        };
        _floatPopup = new Popup { Child = chrome, IsLightDismissEnabled = false };
    }

    private void AttachFloatDrag(FrameworkElement handle)
    {
        handle.PointerPressed += (_, e) =>
        {
            if (_floatPopup is null || !e.GetCurrentPoint(null).Properties.IsLeftButtonPressed || e.OriginalSource is Button)
            {
                return;
            }

            _floatDragging = true;
            _floatDragStart = e.GetCurrentPoint(null).Position;
            handle.CapturePointer(e.Pointer);
            e.Handled = true;
        };
        handle.PointerMoved += (_, e) =>
        {
            if (!_floatDragging || _floatPopup is null)
            {
                return;
            }

            var current = e.GetCurrentPoint(null).Position;
            FloatingPosition = new Point(FloatingPosition.X + (current.X - _floatDragStart.X), FloatingPosition.Y + (current.Y - _floatDragStart.Y));
            _floatDragStart = current;
            if (_floatPopup.XamlRoot is { } root)
            {
                ClampFloatingPosition(root);
            }

            _floatPopup.HorizontalOffset = FloatingPosition.X;
            _floatPopup.VerticalOffset = FloatingPosition.Y;
            e.Handled = true;
        };

        void EndDrag(object sender, PointerRoutedEventArgs e)
        {
            if (!_floatDragging)
            {
                return;
            }

            _floatDragging = false;
            handle.ReleasePointerCaptures();
            Ribbon?.RaiseStateChanged();
        }

        handle.PointerReleased += EndDrag;
        handle.PointerCaptureLost += EndDrag;
    }

    private void ClampFloatingPosition(XamlRoot root)
    {
        var size = root.Size;
        var width = _floatPopup?.Child is FrameworkElement c && c.ActualWidth > 0 ? c.ActualWidth : 120;
        var x = Math.Clamp(FloatingPosition.X, 0, Math.Max(0, size.Width - Math.Min(width, size.Width)));
        var y = Math.Clamp(FloatingPosition.Y, 0, Math.Max(0, size.Height - 40));
        FloatingPosition = new Point(x, y);
    }

    // Dragging a panel by its title away from the ribbon floats it (when the ribbon allows floating panels).
    private void OnCaptionPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (Ribbon?.CanFloatGroups != true || !e.GetCurrentPoint(null).Properties.IsLeftButtonPressed || e.OriginalSource is Button || _captionRow is null)
        {
            return;
        }

        _captionDragStart = e.GetCurrentPoint(null).Position;
        _captionRow.CapturePointer(e.Pointer);
    }

    private void OnCaptionPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (_captionDragStart is not { } start)
        {
            return;
        }

        var current = e.GetCurrentPoint(null).Position;
        if (Math.Abs(current.Y - start.Y) > 16 || Math.Abs(current.X - start.X) > 24)
        {
            _captionDragStart = null;
            _captionRow?.ReleasePointerCaptures();
            Float(new Point(current.X - (ActualWidth / 2), current.Y - 10));
        }
    }

    private void OnCaptionPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (_captionDragStart is not null)
        {
            _captionDragStart = null;
            _captionRow?.ReleasePointerCaptures();
        }
    }

    private void OnGroupRightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        if (e.Handled || Ribbon is not { } ribbon)
        {
            return;
        }

        e.Handled = ribbon.ShowItemContextMenu(this, e.GetPosition(this));
    }
}
