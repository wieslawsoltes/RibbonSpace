using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using RibbonSpace.Commands;
using RibbonSpace.Localization;

namespace RibbonSpace.Controls;

/// <summary>
/// Office-style title bar: application icon, Quick Access Toolbar (of the linked ribbon), document title,
/// centered command search and end content (account, share, window buttons area).
/// </summary>
public partial class RibbonTitleBar : Control
{
    /// <summary>Identifies <see cref="Ribbon"/>.</summary>
    public static readonly DependencyProperty RibbonProperty = DependencyProperty.Register(nameof(Ribbon), typeof(Ribbon), typeof(RibbonTitleBar), new PropertyMetadata(null, (d, e) => ((RibbonTitleBar)d).OnRibbonChanged((Ribbon?)e.OldValue, (Ribbon?)e.NewValue)));

    /// <summary>Identifies <see cref="Title"/>.</summary>
    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(nameof(Title), typeof(string), typeof(RibbonTitleBar), new PropertyMetadata(null, (d, _) => ((RibbonTitleBar)d).UpdateDisplayTitle()));

    /// <summary>Identifies <see cref="DisplayTitle"/>.</summary>
    public static readonly DependencyProperty DisplayTitleProperty = DependencyProperty.Register(nameof(DisplayTitle), typeof(string), typeof(RibbonTitleBar), new PropertyMetadata(null));

    /// <summary>Identifies <see cref="Subtitle"/>.</summary>
    public static readonly DependencyProperty SubtitleProperty = DependencyProperty.Register(nameof(Subtitle), typeof(string), typeof(RibbonTitleBar), new PropertyMetadata(null));

    /// <summary>Identifies <see cref="AppIcon"/>.</summary>
    public static readonly DependencyProperty AppIconProperty = DependencyProperty.Register(nameof(AppIcon), typeof(object), typeof(RibbonTitleBar), new PropertyMetadata(null));

    /// <summary>Identifies <see cref="StartContent"/>.</summary>
    public static readonly DependencyProperty StartContentProperty = DependencyProperty.Register(nameof(StartContent), typeof(object), typeof(RibbonTitleBar), new PropertyMetadata(null));

    /// <summary>Identifies <see cref="EndContent"/>.</summary>
    public static readonly DependencyProperty EndContentProperty = DependencyProperty.Register(nameof(EndContent), typeof(object), typeof(RibbonTitleBar), new PropertyMetadata(null));

    /// <summary>Identifies <see cref="IsSearchVisible"/>.</summary>
    public static readonly DependencyProperty IsSearchVisibleProperty = DependencyProperty.Register(nameof(IsSearchVisible), typeof(bool), typeof(RibbonTitleBar), new PropertyMetadata(true, (d, _) => ((RibbonTitleBar)d).UpdateAdaptive()));

    /// <summary>Identifies <see cref="CaptionButtonsInset"/>.</summary>
    public static readonly DependencyProperty CaptionButtonsInsetProperty = DependencyProperty.Register(nameof(CaptionButtonsInset), typeof(Thickness), typeof(RibbonTitleBar), new PropertyMetadata(new Thickness(0)));

    /// <summary>Identifies <see cref="SearchWidth"/>.</summary>
    public static readonly DependencyProperty SearchWidthProperty = DependencyProperty.Register(nameof(SearchWidth), typeof(double), typeof(RibbonTitleBar), new PropertyMetadata(340d));

    private Border? _quickAccessHost;
    private RibbonSearchBox? _search;
    private FrameworkElement? _titleElement;
    private FrameworkElement? _dragRegion;
    private RibbonQuickAccessToolBar? _hostedQuickAccess;
    private Window? _window;
    private Ribbon? _modelRibbon;
    private Model.RibbonModel? _watchedModel;
    private long _modelToken = -1;

    /// <summary>Creates a title bar.</summary>
    public RibbonTitleBar()
    {
        DefaultStyleKey = typeof(RibbonTitleBar);
        RibbonTheme.EnsureResources();
        IsTabStop = false;
        SizeChanged += (_, _) =>
        {
            UpdateAdaptive();
            UpdatePassthroughRegions();
        };
        Loaded += (_, _) => WatchModel();
        Unloaded += (_, _) => UnwatchModel();
    }

    /// <summary>Linked ribbon (its QAT is hosted here when placed above the ribbon; search drives it).</summary>
    public Ribbon? Ribbon { get => (Ribbon?)GetValue(RibbonProperty); set => SetValue(RibbonProperty, value); }

    /// <summary>Document / window title (falls back to <see cref="Model.RibbonModel.Title"/> of the linked ribbon's model).</summary>
    public string? Title { get => (string?)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }

    /// <summary>Title shown by the template: <see cref="Title"/>, or the title of the linked ribbon's model.</summary>
    public string? DisplayTitle { get => (string?)GetValue(DisplayTitleProperty); private set => SetValue(DisplayTitleProperty, value); }

    /// <summary>Secondary text next to the title ("Saved", "Editing").</summary>
    public string? Subtitle { get => (string?)GetValue(SubtitleProperty); set => SetValue(SubtitleProperty, value); }

    /// <summary>Application icon.</summary>
    public object? AppIcon { get => GetValue(AppIconProperty); set => SetValue(AppIconProperty, value); }

    /// <summary>Content after the icon (AutoSave toggle...).</summary>
    public object? StartContent { get => GetValue(StartContentProperty); set => SetValue(StartContentProperty, value); }

    /// <summary>Content at the right (account, share, comments).</summary>
    public object? EndContent { get => GetValue(EndContentProperty); set => SetValue(EndContentProperty, value); }

    /// <summary>Shows the command search box.</summary>
    public bool IsSearchVisible { get => (bool)GetValue(IsSearchVisibleProperty); set => SetValue(IsSearchVisibleProperty, value); }

    /// <summary>Space reserved for OS caption buttons (set automatically by <see cref="AttachToWindow"/>).</summary>
    public Thickness CaptionButtonsInset { get => (Thickness)GetValue(CaptionButtonsInsetProperty); set => SetValue(CaptionButtonsInsetProperty, value); }

    /// <summary>Width of the search box.</summary>
    public double SearchWidth { get => (double)GetValue(SearchWidthProperty); set => SetValue(SearchWidthProperty, value); }

    /// <summary>The search box.</summary>
    public RibbonSearchBox? SearchBox => _search;

    /// <inheritdoc />
    protected override void OnApplyTemplate()
    {
        if (_quickAccessHost is not null && _quickAccessHost.Child is RibbonQuickAccessToolBar)
        {
            _quickAccessHost.Child = null;
        }

        base.OnApplyTemplate();
        _quickAccessHost = GetTemplateChild("PART_QuickAccessHost") as Border;
        _search = GetTemplateChild("PART_SearchBox") as RibbonSearchBox;
        _titleElement = GetTemplateChild("PART_Title") as FrameworkElement;
        _dragRegion = GetTemplateChild("PART_DragRegion") as FrameworkElement;
        if (_search is not null)
        {
            _search.Ribbon = Ribbon;
        }

        HostQuickAccess();
        UpdateAdaptive();
        UpdateDisplayTitle();
        if (_window is not null)
        {
            // AttachToWindow ran before the template was applied: register the real drag region now.
            ApplyWindowTitleBar(_window);
        }
    }

    private void OnRibbonChanged(Ribbon? oldValue, Ribbon? newValue)
    {
        if (oldValue is not null)
        {
            oldValue.QuickAccessChanged -= OnQuickAccessChanged;
            oldValue.IsQuickAccessHostedExternally = false;
            ReleaseQuickAccess();
        }

        if (newValue is not null)
        {
            newValue.QuickAccessChanged += OnQuickAccessChanged;
            newValue.IsQuickAccessHostedExternally = true;
        }

        if (_search is not null)
        {
            _search.Ribbon = newValue;
        }

        HostQuickAccess();
        if (IsLoaded)
        {
            WatchModel();
        }
        else
        {
            UpdateDisplayTitle();
        }
    }

    private void OnQuickAccessChanged(object? sender, EventArgs e) => HostQuickAccess();

    private void HostQuickAccess()
    {
        if (_quickAccessHost is null)
        {
            return;
        }

        var ribbon = Ribbon;
        var qat = ribbon?.QuickAccessToolBar;
        var show = ribbon is { IsQuickAccessVisible: true, QuickAccessPosition: RibbonQuickAccessPosition.AboveRibbon } && qat is not null;
        if (!show)
        {
            ReleaseQuickAccess();
            ribbon?.UpdateQuickAccessPlacement();
            return;
        }

        if (!ReferenceEquals(_hostedQuickAccess, qat))
        {
            ReleaseQuickAccess();
        }

        if (!ReferenceEquals(_quickAccessHost.Child, qat))
        {
            if (VisualTreeHelper.GetParent(qat!) is Border other)
            {
                other.Child = null;
            }

            _quickAccessHost.Child = qat;
        }

        // The QAT follows the title bar foreground while hosted here (cleared when it moves back below the ribbon).
        _hostedQuickAccess = qat;
        qat!.SetBinding(ForegroundProperty, new Microsoft.UI.Xaml.Data.Binding { Source = this, Path = new PropertyPath(nameof(Foreground)) });
        UpdatePassthroughRegions();
    }

    private void ReleaseQuickAccess()
    {
        if (_quickAccessHost?.Child is RibbonQuickAccessToolBar)
        {
            _quickAccessHost.Child = null;
        }

        _hostedQuickAccess?.ClearValue(ForegroundProperty);
        _hostedQuickAccess = null;
    }

    private void WatchModel()
    {
        UnwatchModel();
        if (Ribbon is { } ribbon)
        {
            _modelRibbon = ribbon;
            _modelToken = ribbon.RegisterPropertyChangedCallback(Ribbon.ModelProperty, (_, _) => WatchModel());
            _watchedModel = ribbon.Model;
            if (_watchedModel is not null)
            {
                _watchedModel.PropertyChanged += OnModelPropertyChanged;
            }
        }

        UpdateDisplayTitle();
    }

    private void UnwatchModel()
    {
        if (_modelRibbon is not null && _modelToken >= 0)
        {
            _modelRibbon.UnregisterPropertyChangedCallback(Ribbon.ModelProperty, _modelToken);
        }

        if (_watchedModel is not null)
        {
            _watchedModel.PropertyChanged -= OnModelPropertyChanged;
        }

        _modelRibbon = null;
        _modelToken = -1;
        _watchedModel = null;
    }

    private void OnModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrEmpty(e.PropertyName) || e.PropertyName == nameof(Model.RibbonModel.Title))
        {
            if (DispatcherQueue is { HasThreadAccess: false } queue)
            {
                queue.TryEnqueue(UpdateDisplayTitle);
            }
            else
            {
                UpdateDisplayTitle();
            }
        }
    }

    private void UpdateDisplayTitle() => DisplayTitle = Title ?? Ribbon?.Model?.Title;

    private void UpdateAdaptive()
    {
        if (_search is not null)
        {
            _search.Visibility = IsSearchVisible && ActualWidth > 720 ? Visibility.Visible : Visibility.Collapsed;
            _search.Width = Math.Min(SearchWidth, Math.Max(180, (ActualWidth - 520) / 2));
        }

        if (_titleElement is not null)
        {
            _titleElement.Visibility = ActualWidth is 0 or > 480 ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    /// <summary>
    /// Extends the window content into the OS title bar and uses the empty area of this control as drag region
    /// (where supported); interactive parts (QAT, search, start / end content) keep receiving input. Reserves space
    /// for the caption buttons (system insets on Windows; left on macOS, right elsewhere; mirrored for RTL).
    /// </summary>
    public void AttachToWindow(Window window)
    {
        ArgumentNullException.ThrowIfNull(window);
        _window = window;
        ApplyWindowTitleBar(window);
    }

    private void ApplyWindowTitleBar(Window window)
    {
        try
        {
            window.ExtendsContentIntoTitleBar = true;

            // Only the dedicated empty drag element: the template root also hosts the QAT, search and content.
            window.SetTitleBar(_dragRegion ?? GetTemplateChild("PART_DragRegion") as UIElement ?? this);
            CaptionButtonsInset = ComputeCaptionInset(window);
            UpdatePassthroughRegions();
        }
        catch (Exception ex) when (ex is NotSupportedException or NotImplementedException or InvalidOperationException or COMException)
        {
            CaptionButtonsInset = new Thickness(0);
        }
    }

    private Thickness ComputeCaptionInset(Window window)
    {
        double left;
        double right;
#if WINDOWS
        var scale = XamlRoot is { RasterizationScale: > 0 } root ? root.RasterizationScale : 1d;
        left = window.AppWindow.TitleBar.LeftInset / scale;
        right = window.AppWindow.TitleBar.RightInset / scale;
#else
        _ = window;
        var mac = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
        left = mac ? 76 : 0;
        right = mac ? 0 : 140;
#endif

        // Padding is mirrored in right-to-left layouts while the caption buttons are not.
        return FlowDirection == FlowDirection.RightToLeft ? new Thickness(right, 0, left, 0) : new Thickness(left, 0, right, 0);
    }

    private void UpdatePassthroughRegions()
    {
#if WINDOWS
        if (_window is null || XamlRoot is null)
        {
            return;
        }

        try
        {
            var scale = XamlRoot.RasterizationScale;
            var rects = new List<Windows.Graphics.RectInt32>();
            foreach (var part in new FrameworkElement?[] { _quickAccessHost, _search, GetTemplateChild("PART_StartContent") as FrameworkElement, GetTemplateChild("PART_EndContent") as FrameworkElement })
            {
                if (part is not { Visibility: Visibility.Visible, ActualWidth: > 0, ActualHeight: > 0 })
                {
                    continue;
                }

                var origin = part.TransformToVisual(null).TransformPoint(new Windows.Foundation.Point(0, 0));
                rects.Add(new Windows.Graphics.RectInt32((int)(origin.X * scale), (int)(origin.Y * scale), (int)(part.ActualWidth * scale), (int)(part.ActualHeight * scale)));
            }

            Microsoft.UI.Input.InputNonClientPointerSource.GetForWindowId(_window.AppWindow.Id).SetRegionRects(Microsoft.UI.Input.NonClientRegionKind.Passthrough, rects.ToArray());
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or COMException)
        {
            // Older Windows App SDK / not yet laid out.
        }
#endif
    }
}

/// <summary>Status bar with start and end zones (page count, word count, view buttons, zoom).</summary>
[Microsoft.UI.Xaml.Markup.ContentProperty(Name = nameof(Items))]
public partial class RibbonStatusBar : Control, IRibbonLayoutHost, IRibbonItemOwner
{
    /// <summary>Identifies <see cref="CommandCatalog"/>.</summary>
    public static readonly DependencyProperty CommandCatalogProperty = DependencyProperty.Register(nameof(CommandCatalog), typeof(RibbonCommandCatalog), typeof(RibbonStatusBar), new PropertyMetadata(null, (d, _) => ((RibbonStatusBar)d).ResolveCommands()));

    /// <summary>Identifies <see cref="Ribbon"/>.</summary>
    public static readonly DependencyProperty RibbonProperty = DependencyProperty.Register(nameof(Ribbon), typeof(Ribbon), typeof(RibbonStatusBar), new PropertyMetadata(null, (d, _) => ((RibbonStatusBar)d).ResolveCommands()));

    private static readonly RibbonMetrics StatusMetrics = RibbonMetrics.Compact with { SimplifiedItemHeight = 22 };
    private StackPanel? _start;
    private StackPanel? _end;

    /// <summary>Creates a status bar.</summary>
    public RibbonStatusBar()
    {
        DefaultStyleKey = typeof(RibbonStatusBar);
        RibbonTheme.EnsureResources();
        Items.CollectionChanged += (_, _) => Sync();
        EndItems.CollectionChanged += (_, _) => Sync();
        IsTabStop = false;
    }

    /// <summary>Raised when an item is invoked.</summary>
    public event EventHandler<RibbonItemInvokedEventArgs>? ItemInvoked;

    /// <summary>Items at the start (left).</summary>
    public System.Collections.ObjectModel.ObservableCollection<UIElement> Items { get; } = [];

    /// <summary>Items at the end (right).</summary>
    public System.Collections.ObjectModel.ObservableCollection<UIElement> EndItems { get; } = [];

    /// <summary>Command catalog for <c>CommandId</c> resolution (falls back to the ribbon's).</summary>
    public RibbonCommandCatalog? CommandCatalog { get => (RibbonCommandCatalog?)GetValue(CommandCatalogProperty); set => SetValue(CommandCatalogProperty, value); }

    /// <summary>Optional ribbon (shared command catalog, item invoked notifications).</summary>
    public Ribbon? Ribbon { get => (Ribbon?)GetValue(RibbonProperty); set => SetValue(RibbonProperty, value); }

    /// <inheritdoc />
    public RibbonMetrics Metrics => StatusMetrics;

    RibbonCommandCatalog? IRibbonItemOwner.CommandCatalog => CommandCatalog ?? Ribbon?.CommandCatalog;

    Ribbon? IRibbonItemOwner.OwnerRibbon => Ribbon;

    /// <inheritdoc />
    public void OnItemInvoked(FrameworkElement item, string? commandId, object? parameter)
    {
        ItemInvoked?.Invoke(this, new RibbonItemInvokedEventArgs(item, commandId, parameter));
        Ribbon?.OnItemInvoked(item, commandId, parameter);
    }

    /// <inheritdoc />
    protected override void OnApplyTemplate()
    {
        _start?.Children.Clear();
        _end?.Children.Clear();
        base.OnApplyTemplate();
        _start = GetTemplateChild("PART_StartItems") as StackPanel;
        _end = GetTemplateChild("PART_EndItems") as StackPanel;
        Sync();
    }

    private void Sync()
    {
        Fill(_start, Items);
        Fill(_end, EndItems);
        ResolveCommands();
    }

    private void ResolveCommands()
    {
        foreach (var item in Items.Concat(EndItems).OfType<FrameworkElement>().SelectMany(RibbonGroup.Flatten))
        {
            RibbonItemHelper.ResolveCommand(item);
        }
    }

    private void Fill(StackPanel? host, IEnumerable<UIElement> items)
    {
        foreach (var item in items)
        {
            RibbonItemHelper.SetOwner(item, this);
        }

        if (host is null)
        {
            return;
        }

        host.Children.Clear();
        foreach (var item in items)
        {
            if (VisualTreeHelper.GetParent(item) is Panel parent)
            {
                parent.Children.Remove(item);
            }

            if (item is IRibbonItem ribbonItem)
            {
                ribbonItem.ApplyLayout(new RibbonItemLayout(RibbonItemSize.Small, StatusMetrics, true, true));
            }

            host.Children.Add(item);
        }
    }

    /// <inheritdoc />
    public void InvalidateItemsLayout() => InvalidateMeasure();
}

/// <summary>Office status bar zoom control ([-] slider [+] 100%).</summary>
/// <remarks>The command is executed only for user changes (buttons, slider), never when <see cref="Value"/> is set in code.</remarks>
public partial class RibbonZoomControl : RibbonControlBase
{
    /// <summary>Identifies <see cref="Value"/>.</summary>
    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(nameof(Value), typeof(double), typeof(RibbonZoomControl), new PropertyMetadata(100d, (d, e) => ((RibbonZoomControl)d).OnValueChanged((double)e.NewValue)));

    /// <summary>Identifies <see cref="Minimum"/>.</summary>
    public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register(nameof(Minimum), typeof(double), typeof(RibbonZoomControl), new PropertyMetadata(10d, (d, _) => ((RibbonZoomControl)d).OnRangeChanged()));

    /// <summary>Identifies <see cref="Maximum"/>.</summary>
    public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(nameof(Maximum), typeof(double), typeof(RibbonZoomControl), new PropertyMetadata(500d, (d, _) => ((RibbonZoomControl)d).OnRangeChanged()));

    /// <summary>Identifies <see cref="Step"/>.</summary>
    public static readonly DependencyProperty StepProperty = DependencyProperty.Register(nameof(Step), typeof(double), typeof(RibbonZoomControl), new PropertyMetadata(10d));

    /// <summary>Identifies <see cref="ValueText"/>.</summary>
    public static readonly DependencyProperty ValueTextProperty = DependencyProperty.Register(nameof(ValueText), typeof(string), typeof(RibbonZoomControl), new PropertyMetadata("100%"));

    private Button? _zoomOut;
    private Button? _zoomIn;
    private Button? _percent;
    private Slider? _slider;
    private bool _syncingSlider;
    private bool _userChange;

    /// <summary>Creates a zoom control.</summary>
    public RibbonZoomControl()
    {
        DefaultStyleKey = typeof(RibbonZoomControl);
        CanAddToQuickAccess = false;
        IsTabStop = false;
    }

    /// <summary>Raised when the percentage button is clicked (show a Zoom dialog).</summary>
    public event EventHandler? ZoomDialogRequested;

    /// <summary>Raised when the value changes (user or code).</summary>
    public event EventHandler<double>? ValueChanged;

    /// <summary>Zoom percentage (clamped to <see cref="Minimum"/> .. <see cref="Maximum"/>).</summary>
    public double Value { get => (double)GetValue(ValueProperty); set => SetValue(ValueProperty, value); }

    /// <summary>Minimum.</summary>
    public double Minimum { get => (double)GetValue(MinimumProperty); set => SetValue(MinimumProperty, value); }

    /// <summary>Maximum.</summary>
    public double Maximum { get => (double)GetValue(MaximumProperty); set => SetValue(MaximumProperty, value); }

    /// <summary>Step of the +/- buttons.</summary>
    public double Step { get => (double)GetValue(StepProperty); set => SetValue(StepProperty, value); }

    /// <summary>Formatted value (template use).</summary>
    public string ValueText { get => (string)GetValue(ValueTextProperty); private set => SetValue(ValueTextProperty, value); }

    /// <inheritdoc />
    protected override void OnApplyTemplate()
    {
        if (_zoomOut is not null)
        {
            _zoomOut.Click -= OnZoomOutClick;
        }

        if (_zoomIn is not null)
        {
            _zoomIn.Click -= OnZoomInClick;
        }

        if (_percent is not null)
        {
            _percent.Click -= OnPercentClick;
        }

        if (_slider is not null)
        {
            _slider.ValueChanged -= OnSliderValueChanged;
        }

        base.OnApplyTemplate();
        _zoomOut = GetTemplateChild("PART_ZoomOut") as Button;
        _zoomIn = GetTemplateChild("PART_ZoomIn") as Button;
        _percent = GetTemplateChild("PART_Percent") as Button;
        _slider = GetTemplateChild("PART_Slider") as Slider;
        if (_zoomOut is not null)
        {
            _zoomOut.Click += OnZoomOutClick;
            AutomationProperties.SetName(_zoomOut, RibbonStrings.Current.ZoomOut);
            ToolTipService.SetToolTip(_zoomOut, RibbonStrings.Current.ZoomOut);
        }

        if (_zoomIn is not null)
        {
            _zoomIn.Click += OnZoomInClick;
            AutomationProperties.SetName(_zoomIn, RibbonStrings.Current.ZoomIn);
            ToolTipService.SetToolTip(_zoomIn, RibbonStrings.Current.ZoomIn);
        }

        if (_percent is not null)
        {
            _percent.Click += OnPercentClick;
            AutomationProperties.SetName(_percent, RibbonStrings.Current.Zoom);
        }

        if (_slider is not null)
        {
            AutomationProperties.SetName(_slider, RibbonStrings.Current.Zoom);
            SyncSlider();
            _slider.ValueChanged += OnSliderValueChanged;
        }
    }

    private void OnZoomOutClick(object sender, RoutedEventArgs e) => SetUserValue(Math.Ceiling((Value - Step) / Step) * Step);

    private void OnZoomInClick(object sender, RoutedEventArgs e) => SetUserValue(Math.Floor((Value + Step) / Step) * Step);

    private void OnPercentClick(object sender, RoutedEventArgs e) => ZoomDialogRequested?.Invoke(this, EventArgs.Empty);

    private void OnSliderValueChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        if (!_syncingSlider)
        {
            SetUserValue(e.NewValue);
        }
    }

    private void SetUserValue(double value)
    {
        _userChange = true;
        try
        {
            Value = value;
        }
        finally
        {
            _userChange = false;
        }
    }

    private void OnRangeChanged()
    {
        SyncSlider();
        var clamped = Clamp(Value);
        if (clamped != Value)
        {
            Value = clamped;
        }
    }

    private double Clamp(double value) => Maximum >= Minimum ? Math.Clamp(value, Minimum, Maximum) : Minimum;

    private void OnValueChanged(double value)
    {
        var clamped = Clamp(value);
        if (clamped != value)
        {
            // Coerce (also for bindings / styles that bypass the CLR setter); the nested change does the work.
            Value = clamped;
            return;
        }

        ValueText = $"{value:0}%";
        SyncSlider();
        ValueChanged?.Invoke(this, value);
        if (_userChange)
        {
            ExecuteCommand(value);
        }
    }

    private void SyncSlider()
    {
        if (_slider is null)
        {
            return;
        }

        _syncingSlider = true;
        try
        {
            _slider.Minimum = Minimum;
            _slider.Maximum = Math.Max(Minimum, Maximum);
            _slider.Value = Value;
        }
        finally
        {
            _syncingSlider = false;
        }
    }

    /// <inheritdoc />
    protected override bool InvokeCore()
    {
        ZoomDialogRequested?.Invoke(this, EventArgs.Empty);
        return true;
    }
}
