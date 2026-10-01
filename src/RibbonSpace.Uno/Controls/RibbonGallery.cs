using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Input;
using Microsoft.UI.Text;
using FontWeight = Windows.UI.Text.FontWeight;
using FontStyle = Windows.UI.Text.FontStyle;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using RibbonSpace.Controls.Primitives;
using RibbonSpace.Localization;
using Windows.Foundation;

namespace RibbonSpace.Controls;

/// <summary>Item of a <see cref="RibbonGallery"/> with a text / color preview (styles, themes, shape styles).</summary>
public partial class RibbonGalleryItem : DependencyObject
{
    /// <summary>Identifies <see cref="Label"/>.</summary>
    public static readonly DependencyProperty LabelProperty = DependencyProperty.Register(nameof(Label), typeof(string), typeof(RibbonGalleryItem), new PropertyMetadata(null));

    /// <summary>Identifies <see cref="Icon"/>.</summary>
    public static readonly DependencyProperty IconProperty = DependencyProperty.Register(nameof(Icon), typeof(object), typeof(RibbonGalleryItem), new PropertyMetadata(null));

    /// <summary>Identifies <see cref="Category"/>.</summary>
    public static readonly DependencyProperty CategoryProperty = DependencyProperty.Register(nameof(Category), typeof(string), typeof(RibbonGalleryItem), new PropertyMetadata(null));

    /// <summary>Identifies <see cref="Value"/>.</summary>
    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(nameof(Value), typeof(object), typeof(RibbonGalleryItem), new PropertyMetadata(null));

    /// <summary>Identifies <see cref="PreviewText"/>.</summary>
    public static readonly DependencyProperty PreviewTextProperty = DependencyProperty.Register(nameof(PreviewText), typeof(string), typeof(RibbonGalleryItem), new PropertyMetadata(null));

    /// <summary>Identifies <see cref="PreviewForeground"/>.</summary>
    public static readonly DependencyProperty PreviewForegroundProperty = DependencyProperty.Register(nameof(PreviewForeground), typeof(Brush), typeof(RibbonGalleryItem), new PropertyMetadata(null));

    /// <summary>Identifies <see cref="PreviewBackground"/>.</summary>
    public static readonly DependencyProperty PreviewBackgroundProperty = DependencyProperty.Register(nameof(PreviewBackground), typeof(Brush), typeof(RibbonGalleryItem), new PropertyMetadata(null));

    /// <summary>Identifies <see cref="PreviewFontFamily"/>.</summary>
    public static readonly DependencyProperty PreviewFontFamilyProperty = DependencyProperty.Register(nameof(PreviewFontFamily), typeof(FontFamily), typeof(RibbonGalleryItem), new PropertyMetadata(null));

    /// <summary>Identifies <see cref="PreviewFontSize"/>.</summary>
    public static readonly DependencyProperty PreviewFontSizeProperty = DependencyProperty.Register(nameof(PreviewFontSize), typeof(double), typeof(RibbonGalleryItem), new PropertyMetadata(14d));

    /// <summary>Identifies <see cref="PreviewFontWeight"/>.</summary>
    public static readonly DependencyProperty PreviewFontWeightProperty = DependencyProperty.Register(nameof(PreviewFontWeight), typeof(FontWeight), typeof(RibbonGalleryItem), new PropertyMetadata(FontWeights.Normal));

    /// <summary>Identifies <see cref="PreviewFontStyle"/>.</summary>
    public static readonly DependencyProperty PreviewFontStyleProperty = DependencyProperty.Register(nameof(PreviewFontStyle), typeof(FontStyle), typeof(RibbonGalleryItem), new PropertyMetadata(FontStyle.Normal));

    /// <summary>Label below / in the preview.</summary>
    public string? Label { get => (string?)GetValue(LabelProperty); set => SetValue(LabelProperty, value); }

    /// <summary>Icon preview.</summary>
    public object? Icon { get => GetValue(IconProperty); set => SetValue(IconProperty, value); }

    /// <summary>Category of the expanded gallery.</summary>
    public string? Category { get => (string?)GetValue(CategoryProperty); set => SetValue(CategoryProperty, value); }

    /// <summary>Value passed to the command.</summary>
    public object? Value { get => GetValue(ValueProperty); set => SetValue(ValueProperty, value); }

    /// <summary>Text preview ("AaBbCcDd").</summary>
    public string? PreviewText { get => (string?)GetValue(PreviewTextProperty); set => SetValue(PreviewTextProperty, value); }

    /// <summary>Text preview brush.</summary>
    public Brush? PreviewForeground { get => (Brush?)GetValue(PreviewForegroundProperty); set => SetValue(PreviewForegroundProperty, value); }

    /// <summary>Preview background (swatches, themes).</summary>
    public Brush? PreviewBackground { get => (Brush?)GetValue(PreviewBackgroundProperty); set => SetValue(PreviewBackgroundProperty, value); }

    /// <summary>Text preview font.</summary>
    public FontFamily? PreviewFontFamily { get => (FontFamily?)GetValue(PreviewFontFamilyProperty); set => SetValue(PreviewFontFamilyProperty, value); }

    /// <summary>Text preview size.</summary>
    public double PreviewFontSize { get => (double)GetValue(PreviewFontSizeProperty); set => SetValue(PreviewFontSizeProperty, value); }

    /// <summary>Text preview weight.</summary>
    public FontWeight PreviewFontWeight { get => (FontWeight)GetValue(PreviewFontWeightProperty); set => SetValue(PreviewFontWeightProperty, value); }

    /// <summary>Text preview style.</summary>
    public FontStyle PreviewFontStyle { get => (FontStyle)GetValue(PreviewFontStyleProperty); set => SetValue(PreviewFontStyleProperty, value); }
}

/// <summary>
/// In-ribbon gallery (Styles, Shape Styles, Themes, Transitions): shows a row-scrolled window of previews,
/// adapts its column count to the group state, collapses into a drop-down button, and expands into a
/// categorized, filterable popup with footer commands. Supports live preview.
/// </summary>
[ContentProperty(Name = nameof(Items))]
public partial class RibbonGallery : RibbonControlBase
{
    /// <summary>Identifies <see cref="ItemsSource"/>.</summary>
    public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(nameof(ItemsSource), typeof(object), typeof(RibbonGallery), new PropertyMetadata(null, (d, _) => ((RibbonGallery)d).OnItemsSourceChanged()));

    // Marks the Viewbox hosting a UIElement item so it can release the element before containers are rebuilt.
    private static readonly object ElementHostTag = new();

    /// <summary>Identifies <see cref="SelectedItem"/>.</summary>
    public static readonly DependencyProperty SelectedItemProperty = DependencyProperty.Register(nameof(SelectedItem), typeof(object), typeof(RibbonGallery), new PropertyMetadata(null, (d, _) => ((RibbonGallery)d).UpdateSelection()));

    /// <summary>Identifies <see cref="ItemTemplate"/>.</summary>
    public static readonly DependencyProperty ItemTemplateProperty = DependencyProperty.Register(nameof(ItemTemplate), typeof(DataTemplate), typeof(RibbonGallery), new PropertyMetadata(null, (d, _) => ((RibbonGallery)d).Rebuild()));

    /// <summary>Identifies <see cref="ItemWidth"/>.</summary>
    public static readonly DependencyProperty ItemWidthProperty = DependencyProperty.Register(nameof(ItemWidth), typeof(double), typeof(RibbonGallery), new PropertyMetadata(72d, (d, _) => ((RibbonGallery)d).OnLayoutPropertyChanged()));

    /// <summary>Identifies <see cref="ItemHeight"/>.</summary>
    public static readonly DependencyProperty ItemHeightProperty = DependencyProperty.Register(nameof(ItemHeight), typeof(double), typeof(RibbonGallery), new PropertyMetadata(62d, (d, _) => ((RibbonGallery)d).OnLayoutPropertyChanged()));

    /// <summary>Identifies <see cref="MinColumns"/>.</summary>
    public static readonly DependencyProperty MinColumnsProperty = DependencyProperty.Register(nameof(MinColumns), typeof(int), typeof(RibbonGallery), new PropertyMetadata(3, (d, _) => ((RibbonGallery)d).OnLayoutPropertyChanged()));

    /// <summary>Identifies <see cref="MaxColumns"/>.</summary>
    public static readonly DependencyProperty MaxColumnsProperty = DependencyProperty.Register(nameof(MaxColumns), typeof(int), typeof(RibbonGallery), new PropertyMetadata(6, (d, _) => ((RibbonGallery)d).OnLayoutPropertyChanged()));

    /// <summary>Identifies <see cref="Rows"/>.</summary>
    public static readonly DependencyProperty RowsProperty = DependencyProperty.Register(nameof(Rows), typeof(int), typeof(RibbonGallery), new PropertyMetadata(1, (d, _) => ((RibbonGallery)d).OnLayoutPropertyChanged()));

    /// <summary>Identifies <see cref="DropDownColumns"/>.</summary>
    public static readonly DependencyProperty DropDownColumnsProperty = DependencyProperty.Register(nameof(DropDownColumns), typeof(int), typeof(RibbonGallery), new PropertyMetadata(0));

    /// <summary>Identifies <see cref="PreviewCommand"/>.</summary>
    public static readonly DependencyProperty PreviewCommandProperty = DependencyProperty.Register(nameof(PreviewCommand), typeof(ICommand), typeof(RibbonGallery), new PropertyMetadata(null));

    /// <summary>Identifies <see cref="IsFilterEnabled"/>.</summary>
    public static readonly DependencyProperty IsFilterEnabledProperty = DependencyProperty.Register(nameof(IsFilterEnabled), typeof(bool), typeof(RibbonGallery), new PropertyMetadata(false));

    /// <summary>Identifies <see cref="ShowItemLabels"/>.</summary>
    public static readonly DependencyProperty ShowItemLabelsProperty = DependencyProperty.Register(nameof(ShowItemLabels), typeof(bool), typeof(RibbonGallery), new PropertyMetadata(true, (d, _) => ((RibbonGallery)d).Rebuild()));

    /// <summary>Identifies <see cref="IsDropDownOnly"/>.</summary>
    public static readonly DependencyProperty IsDropDownOnlyProperty = DependencyProperty.Register(nameof(IsDropDownOnly), typeof(bool), typeof(RibbonGallery), new PropertyMetadata(false, (d, _) => ((RibbonGallery)d).OnLayoutPropertyChanged()));

    /// <summary>Identifies <see cref="MaxDropDownHeight"/>.</summary>
    public static readonly DependencyProperty MaxDropDownHeightProperty = DependencyProperty.Register(nameof(MaxDropDownHeight), typeof(double), typeof(RibbonGallery), new PropertyMetadata(460d));

    private readonly RibbonUniformGridPanel _inlinePanel = new() { Spacing = 2 };
    private Border? _inlineHost;
    private FrameworkElement? _inlineRoot;
    private RibbonButton? _dropDownButton;
    private Button? _upButton;
    private Button? _downButton;
    private Button? _moreButton;
    private Popup? _popup;
    private Control? _opener;
    private INotifyCollectionChanged? _observedSource;
    private object[] _builtItems = [];
    private int _columns = 6;
    private bool _previewing;
    private bool _previewEndPending;

    /// <summary>Creates a gallery.</summary>
    public RibbonGallery()
    {
        DefaultStyleKey = typeof(RibbonGallery);
        Size = RibbonItemSize.Large;
        Items.CollectionChanged += (_, _) => Rebuild();
        IsTabStop = false;

        // The items source is only observed while loaded, so a long-lived view-model collection does not keep the
        // gallery alive; changes made meanwhile are picked up when it is loaded again.
        Loaded += (_, _) =>
        {
            ObserveSource();
            if (!_builtItems.SequenceEqual(EffectiveItems))
            {
                Rebuild();
            }
        };
        Unloaded += (_, _) =>
        {
            CloseDropDown();
            ObserveSource(detach: true);
        };
    }

    /// <summary>Raised when an item is picked.</summary>
    public event EventHandler<RibbonGalleryItemEventArgs>? ItemClick;

    /// <summary>Raised while hovering items (live preview); the item is null when the preview ends.</summary>
    public event EventHandler<RibbonGalleryItemEventArgs>? ItemPreview;

    /// <summary>Items (used when <see cref="ItemsSource"/> is not set).</summary>
    public ObservableCollection<object> Items { get; } = [];

    /// <summary>Footer entries of the expanded popup (e.g. RibbonButtons "Clear Formatting", "Apply Styles...").</summary>
    public ObservableCollection<UIElement> FooterItems { get; } = [];

    /// <summary>Items source.</summary>
    public object? ItemsSource { get => GetValue(ItemsSourceProperty); set => SetValue(ItemsSourceProperty, value); }

    /// <summary>Selected item.</summary>
    public object? SelectedItem { get => GetValue(SelectedItemProperty); set => SetValue(SelectedItemProperty, value); }

    /// <summary>Template for data items.</summary>
    public DataTemplate? ItemTemplate { get => (DataTemplate?)GetValue(ItemTemplateProperty); set => SetValue(ItemTemplateProperty, value); }

    /// <summary>Item width.</summary>
    public double ItemWidth { get => (double)GetValue(ItemWidthProperty); set => SetValue(ItemWidthProperty, value); }

    /// <summary>Item height.</summary>
    public double ItemHeight { get => (double)GetValue(ItemHeightProperty); set => SetValue(ItemHeightProperty, value); }

    /// <summary>Columns in the Medium group state.</summary>
    public int MinColumns { get => (int)GetValue(MinColumnsProperty); set => SetValue(MinColumnsProperty, value); }

    /// <summary>Columns in the Large group state.</summary>
    public int MaxColumns { get => (int)GetValue(MaxColumnsProperty); set => SetValue(MaxColumnsProperty, value); }

    /// <summary>Visible rows in the ribbon.</summary>
    public int Rows { get => (int)GetValue(RowsProperty); set => SetValue(RowsProperty, value); }

    /// <summary>Columns of the expanded popup (0 = max(MaxColumns, 5)).</summary>
    public int DropDownColumns { get => (int)GetValue(DropDownColumnsProperty); set => SetValue(DropDownColumnsProperty, value); }

    /// <summary>Live preview command (item value while hovering, then null).</summary>
    public ICommand? PreviewCommand { get => (ICommand?)GetValue(PreviewCommandProperty); set => SetValue(PreviewCommandProperty, value); }

    /// <summary>Shows a filter box in the popup.</summary>
    public bool IsFilterEnabled { get => (bool)GetValue(IsFilterEnabledProperty); set => SetValue(IsFilterEnabledProperty, value); }

    /// <summary>Shows labels in default item visuals.</summary>
    public bool ShowItemLabels { get => (bool)GetValue(ShowItemLabelsProperty); set => SetValue(ShowItemLabelsProperty, value); }

    /// <summary>Always presented as a drop-down button (never inline).</summary>
    public bool IsDropDownOnly { get => (bool)GetValue(IsDropDownOnlyProperty); set => SetValue(IsDropDownOnlyProperty, value); }

    /// <summary>Maximum popup height.</summary>
    public double MaxDropDownHeight { get => (double)GetValue(MaxDropDownHeightProperty); set => SetValue(MaxDropDownHeightProperty, value); }

    /// <summary>Selects a category for data items (defaults to RibbonGalleryItem.Category / model Category).</summary>
    public Func<object, string?>? CategorySelector { get; set; }

    /// <summary>Effective items.</summary>
    public IReadOnlyList<object> EffectiveItems => ItemsSource is IEnumerable e and not string ? e.Cast<object>().ToArray() : Items.ToArray();

    /// <summary>True when shown inline in the ribbon (not as a button).</summary>
    public bool IsInline => !IsDropDownOnly && CurrentSize != RibbonItemSize.Small && !IsSimplified;

    /// <summary>True while the popup is open.</summary>
    public bool IsDropDownOpen => _popup?.IsOpen == true;

    /// <summary>First visible row of the inline window.</summary>
    public int FirstRow => _inlinePanel.FirstRow;

    /// <summary>Columns currently shown inline.</summary>
    public int Columns => _columns;

    private void OnItemsSourceChanged()
    {
        ObserveSource();
        Rebuild();
    }

    private void ObserveSource(bool detach = false)
    {
        var source = detach || !IsLoaded ? null : ItemsSource as INotifyCollectionChanged;
        if (ReferenceEquals(source, _observedSource))
        {
            return;
        }

        if (_observedSource is not null)
        {
            _observedSource.CollectionChanged -= OnSourceCollectionChanged;
        }

        _observedSource = source;
        if (source is not null)
        {
            source.CollectionChanged += OnSourceCollectionChanged;
        }
    }

    private void OnSourceCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => Rebuild();

    private void OnLayoutPropertyChanged()
    {
        ApplyPresentation();
        RibbonItemHelper.InvalidateHostLayout(this);
    }

    /// <inheritdoc />
    protected override void OnApplyTemplate()
    {
        if (_inlineHost is not null)
        {
            _inlineHost.Child = null;
        }

        if (_dropDownButton is not null)
        {
            _dropDownButton.Click -= OnOpenClick;
        }

        if (_moreButton is not null)
        {
            _moreButton.Click -= OnOpenClick;
        }

        ReleaseElementItems(_inlinePanel);

        if (_upButton is not null)
        {
            _upButton.Click -= OnUp;
        }

        if (_downButton is not null)
        {
            _downButton.Click -= OnDown;
        }

        base.OnApplyTemplate();
        _inlineHost = GetTemplateChild("PART_InlineHost") as Border;
        _inlineRoot = GetTemplateChild("PART_InlineRoot") as FrameworkElement;
        _dropDownButton = GetTemplateChild("PART_DropDownButton") as RibbonButton;
        _upButton = GetTemplateChild("PART_UpButton") as Button;
        _downButton = GetTemplateChild("PART_DownButton") as Button;
        _moreButton = GetTemplateChild("PART_MoreButton") as Button;
        if (_inlineHost is not null)
        {
            _inlineHost.Child = _inlinePanel;
        }

        if (_dropDownButton is not null)
        {
            _dropDownButton.Click += OnOpenClick;
            _dropDownButton.IsChromeButton = true;
        }

        if (_moreButton is not null)
        {
            _moreButton.Click += OnOpenClick;
            ToolTipService.SetToolTip(_moreButton, RibbonStrings.Current.GalleryMore);
            AutomationProperties.SetName(_moreButton, RibbonStrings.Current.GalleryMore);
        }

        if (_upButton is not null)
        {
            _upButton.Click += OnUp;
            AutomationProperties.SetName(_upButton, RibbonStrings.Current.GalleryUp);
        }

        if (_downButton is not null)
        {
            _downButton.Click += OnDown;
            AutomationProperties.SetName(_downButton, RibbonStrings.Current.GalleryDown);
        }

        Rebuild();
    }

    /// <inheritdoc />
    protected override void OnLayoutAppliedCore(RibbonItemLayout layout) => ApplyPresentation();

    /// <inheritdoc />
    protected override void OnRibbonPropertyChanged(DependencyPropertyChangedEventArgs e) => ApplyPresentation();

    private void ApplyPresentation()
    {
        _columns = Math.Max(1, CurrentSize == RibbonItemSize.Medium ? MinColumns : MaxColumns);
        var inline = IsInline;
        if (_inlineRoot is not null)
        {
            _inlineRoot.Visibility = inline ? Visibility.Visible : Visibility.Collapsed;
        }

        if (_dropDownButton is not null)
        {
            _dropDownButton.Visibility = inline ? Visibility.Collapsed : Visibility.Visible;
            _dropDownButton.Label = Label;
            _dropDownButton.Icon = Icon ?? "";
            var buttonSize = IsSimplified ? RibbonItemSize.Small : CurrentSize == RibbonItemSize.Medium && IsDropDownOnly ? RibbonItemSize.Medium : RibbonItemSize.Large;
            _dropDownButton.ApplyLayout(new RibbonItemLayout(buttonSize, Metrics, IsSimplified, IsSimplified ? ShowLabelInSimplifiedMode : buttonSize == RibbonItemSize.Medium ? true : null));
        }

        var rows = Math.Max(1, Rows);
        var available = Math.Max(24, Metrics.GroupContentHeight - 4);
        var height = Math.Min(ItemHeight, (available - ((rows - 1) * 2)) / rows);
        _inlinePanel.Columns = _columns;
        _inlinePanel.ItemWidth = ItemWidth;
        _inlinePanel.ItemHeight = height;
        _inlinePanel.MaxRows = rows;
        ClampFirstRow();
        UpdateScrollButtons();
    }

    private void Rebuild()
    {
        // UIElement items must leave their old (discarded) Viewbox first, or they could not be shown again.
        ReleaseElementItems(_inlinePanel);
        _inlinePanel.Children.Clear();
        _builtItems = EffectiveItems.ToArray();
        foreach (var item in _builtItems)
        {
            _inlinePanel.Children.Add(CreateContainer(item, popup: false));
        }

        ApplyPresentation();
        UpdateSelection();
        RibbonItemHelper.InvalidateHostLayout(this);
    }

    private static void ReleaseElementItems(Panel panel)
    {
        foreach (var container in panel.Children.OfType<ContentControl>())
        {
            if (container.Content is Panel visual)
            {
                foreach (var host in visual.Children.OfType<Viewbox>().Where(v => ReferenceEquals(v.Tag, ElementHostTag)))
                {
                    host.Child = null;
                }
            }
        }
    }

    private Button CreateContainer(object item, bool popup)
    {
        // Only the source gallery's inline containers host UIElement items: popups and linked copies (QAT) show a text
        // fallback instead of stealing the element from the ribbon.
        var hostElements = !popup && RibbonItemHelper.GetSourceItem(this) is null;
        var button = new Button
        {
            Tag = item,
            Content = CreateItemVisual(item, hostElements),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
        };
        if (Application.Current.Resources.TryGetValue("RibbonGalleryItemStyle", out var style))
        {
            button.Style = (Style)style;
        }

        if (popup)
        {
            button.Width = ItemWidth;
            button.Height = ItemHeight;
        }

        var label = GetItemLabel(item);
        AutomationProperties.SetName(button, label ?? string.Empty);
        if (!string.IsNullOrEmpty(label))
        {
            ToolTipService.SetToolTip(button, label);
        }

        button.Click += (_, _) => Pick(item);
        button.PointerEntered += (_, _) => Preview(item);
        button.PointerExited += (_, _) => Preview(null);

        // Keyboard live preview: moving keyboard focus to an item previews it (the programmatic focus set when the popup
        // opens does not); the preview ends when focus leaves the items.
        button.GotFocus += (_, _) =>
        {
            _previewEndPending = false;
            if (button.FocusState == FocusState.Keyboard)
            {
                Preview(item);
            }
        };
        button.LostFocus += (_, _) =>
        {
            _previewEndPending = true;
            DispatcherQueue?.TryEnqueue(() =>
            {
                if (_previewEndPending)
                {
                    _previewEndPending = false;
                    Preview(null);
                }
            });
        };
        button.Loaded += (s, _) => ApplySelectionVisual((Button)s);
        return button;
    }

    private static string? GetItemLabel(object item) => item switch
    {
        RibbonGalleryItem g => g.Label,
        Model.RibbonNodeModel m => m.Label,
        UIElement element => AutomationProperties.GetName(element) is { Length: > 0 } name ? name : (element as FrameworkElement)?.Name is { Length: > 0 } elementName ? elementName : null,
        _ => item.ToString(),
    };

    private object CreateItemVisual(object item, bool hostElements)
    {
        if (item is UIElement element)
        {
            if (hostElements && VisualTreeHelper.GetParent(element) is null && (element as FrameworkElement)?.Parent is null)
            {
                return new Grid { Children = { new Viewbox { Child = element, Tag = ElementHostTag } } };
            }

            return new TextBlock { Text = GetItemLabel(item) ?? element.GetType().Name, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap, TextAlignment = TextAlignment.Center, Margin = new Thickness(2) };
        }

        if (ItemTemplate is not null)
        {
            return new ContentPresenter { Content = item is RibbonGalleryItem g && g.Value is not null ? g.Value : item, ContentTemplate = ItemTemplate, HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch };
        }

        var grid = new Grid { RowDefinitions = { new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }, new RowDefinition { Height = GridLength.Auto } } };
        FrameworkElement preview;
        switch (item)
        {
            case RibbonGalleryItem g:
                preview = CreatePreview(g.PreviewText, g.Icon, g.PreviewBackground, g.PreviewForeground, g.PreviewFontFamily, g.PreviewFontSize, g.PreviewFontWeight, g.PreviewFontStyle);
                AddLabel(grid, g.Label);
                break;
            case Model.RibbonGalleryItemModel m:
                preview = CreatePreview(m.Content as string, m.Icon, null, null, null, 14, FontWeights.Normal, FontStyle.Normal);
                AddLabel(grid, m.Label);
                break;
            default:
                preview = new TextBlock { Text = item.ToString(), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap };
                break;
        }

        grid.Children.Insert(0, preview);
        return grid;
    }

    private void AddLabel(Grid grid, string? label)
    {
        if (!ShowItemLabels || string.IsNullOrEmpty(label))
        {
            return;
        }

        var text = new TextBlock { Text = label, FontSize = 11, HorizontalAlignment = HorizontalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis, Margin = new Thickness(2, 0, 2, 2) };
        Grid.SetRow(text, 1);
        grid.Children.Add(text);
    }

    private static FrameworkElement CreatePreview(string? text, object? icon, Brush? background, Brush? foreground, FontFamily? family, double size, FontWeight weight, FontStyle style)
    {
        var border = new Border { Background = background, CornerRadius = new CornerRadius(3), Margin = new Thickness(3) };
        if (!string.IsNullOrEmpty(text))
        {
            var tb = new TextBlock { Text = text, FontSize = size, FontWeight = weight, FontStyle = style, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, TextTrimming = TextTrimming.Clip };
            if (family is not null)
            {
                tb.FontFamily = family;
            }

            if (foreground is not null)
            {
                tb.Foreground = foreground;
            }

            border.Child = tb;
        }
        else if (icon is not null)
        {
            var presenter = new RibbonIconPresenter { Icon = icon, IconSize = 28 };
            if (foreground is not null)
            {
                presenter.Foreground = foreground;
            }
            else
            {
                RibbonTheme.SetThemeBrush(presenter, RibbonIconPresenter.ForegroundProperty, "RibbonIconBrush");
            }

            border.Child = presenter;
        }

        return border;
    }

    private void UpdateSelection()
    {
        foreach (var container in _inlinePanel.Children.OfType<Button>())
        {
            ApplySelectionVisual(container);
        }

        if (_popup is { IsOpen: true, Child: FrameworkElement popupRoot })
        {
            foreach (var container in Descendants(popupRoot).OfType<Button>().Where(b => b.Tag is not null))
            {
                ApplySelectionVisual(container);
            }
        }
    }

    private void ApplySelectionVisual(Button container)
    {
        var selected = IsSelected(container.Tag);
        if (VisualStateManager.GoToState(container, selected ? "Selected" : "Unselected", false) || !container.IsLoaded)
        {
            return;
        }

        // Custom item style without selection states.
        container.BorderBrush = selected ? RibbonTheme.GetBrush(container, "RibbonGalleryItemSelectedBorderBrush") : new SolidColorBrush(Microsoft.UI.Colors.Transparent);
    }

    private static IEnumerable<DependencyObject> Descendants(DependencyObject root)
    {
        var stack = new Stack<DependencyObject>();
        stack.Push(root);
        while (stack.Count > 0)
        {
            var current = stack.Pop();
            yield return current;
            switch (current)
            {
                case Panel panel:
                    foreach (var child in panel.Children)
                    {
                        stack.Push(child);
                    }

                    break;
                case Border border when border.Child is not null:
                    stack.Push(border.Child);
                    break;
                case ScrollViewer viewer when viewer.Content is DependencyObject content:
                    stack.Push(content);
                    break;
            }
        }
    }

    private bool IsSelected(object? item)
        => item is not null && (Equals(item, SelectedItem) || (item is RibbonGalleryItem g && g.Value is not null && Equals(g.Value, SelectedItem)));

    private void ClampFirstRow()
    {
        var maxFirst = Math.Max(0, _inlinePanel.RowCount - Math.Max(1, Rows));
        _inlinePanel.FirstRow = Math.Clamp(_inlinePanel.FirstRow, 0, maxFirst);
        _inlinePanel.InvalidateArrange();
    }

    private void UpdateScrollButtons()
    {
        var maxFirst = Math.Max(0, _inlinePanel.RowCount - Math.Max(1, Rows));
        if (_upButton is not null)
        {
            _upButton.IsEnabled = _inlinePanel.FirstRow > 0;
        }

        if (_downButton is not null)
        {
            _downButton.IsEnabled = _inlinePanel.FirstRow < maxFirst;
        }
    }

    private void OnUp(object sender, RoutedEventArgs e) => ScrollRows(-1);

    private void OnDown(object sender, RoutedEventArgs e) => ScrollRows(1);

    /// <summary>Scrolls the inline window by rows.</summary>
    public void ScrollRows(int delta)
    {
        _inlinePanel.FirstRow += delta;
        ClampFirstRow();
        UpdateScrollButtons();
    }

    private void OnOpenClick(object sender, RoutedEventArgs e)
    {
        var opener = sender as Control;
        DispatcherQueue?.TryEnqueue(() =>
        {
            OpenDropDown();
            _opener = opener;
        });
    }

    /// <summary>Picks an item (selects, executes the command, raises ItemClick, closes the popup).</summary>
    public void Pick(object item)
    {
        Preview(null);
        var value = item is RibbonGalleryItem { Value: not null } g ? g.Value : item;
        SelectedItem = value;
        ItemClick?.Invoke(this, new RibbonGalleryItemEventArgs(item, value));
        ExecuteCommand(value);
        if (IsDropDownOpen)
        {
            CloseDropDown(restoreFocus: true);
        }
    }

    private void Preview(object? item)
    {
        if (item is null && !_previewing)
        {
            return;
        }

        _previewing = item is not null;
        var value = item is RibbonGalleryItem { Value: not null } g ? g.Value : item;
        ItemPreview?.Invoke(this, new RibbonGalleryItemEventArgs(item, value));
        if (PreviewCommand is { } command && command.CanExecute(value))
        {
            command.Execute(value);
        }
    }

    /// <summary>Opens the expanded popup.</summary>
    public void OpenDropDown()
    {
        if (XamlRoot is null || IsDropDownOpen)
        {
            return;
        }

        _opener = null;
        var columns = DropDownColumns > 0 ? DropDownColumns : Math.Max(MaxColumns, 5);
        var root = new StackPanel { Spacing = 4, Padding = new Thickness(4) };

        // Arrow keys move between items (XY focus); Tab moves between the filter, the items and the footer.
        var itemsHost = new StackPanel { Spacing = 2, XYFocusKeyboardNavigation = XYFocusKeyboardNavigationMode.Enabled, TabFocusNavigation = KeyboardNavigationMode.Once };
        TextBox? filter = null;
        if (IsFilterEnabled)
        {
            filter = new TextBox { PlaceholderText = RibbonStrings.Current.GalleryFilter, Margin = new Thickness(2, 2, 2, 4) };
            root.Children.Add(filter);
        }

        void Fill(string? query)
        {
            itemsHost.Children.Clear();
            var items = EffectiveItems.Where(i => string.IsNullOrWhiteSpace(query) || (GetItemLabel(i) ?? string.Empty).Contains(query, StringComparison.CurrentCultureIgnoreCase));
            foreach (var group in items.GroupBy(GetCategory))
            {
                if (!string.IsNullOrEmpty(group.Key))
                {
                    itemsHost.Children.Add(new TextBlock
                    {
                        Text = group.Key,
                        FontWeight = FontWeights.SemiBold,
                        FontSize = 12,
                        Margin = new Thickness(4, 6, 4, 2),
                    });
                }

                var panel = new RibbonUniformGridPanel { Columns = columns, ItemWidth = ItemWidth, ItemHeight = ItemHeight, Spacing = 2 };
                foreach (var item in group)
                {
                    panel.Children.Add(CreateContainer(item, popup: true));
                }

                itemsHost.Children.Add(panel);
            }
        }

        Fill(null);
        if (filter is not null)
        {
            filter.TextChanged += (_, _) => Fill(filter.Text);
        }

        root.Children.Add(new ScrollViewer { Content = itemsHost, MaxHeight = MaxDropDownHeight, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled });
        if (FooterItems.Count > 0)
        {
            var separator = new Microsoft.UI.Xaml.Shapes.Rectangle { Height = 1, Margin = new Thickness(0, 2, 0, 2) };
            RibbonTheme.SetThemeBrush(separator, Microsoft.UI.Xaml.Shapes.Shape.FillProperty, "RibbonSeparatorBrush");
            root.Children.Add(separator);
            foreach (var footer in FooterItems)
            {
                if (VisualTreeHelper.GetParent(footer) is Panel parent)
                {
                    parent.Children.Remove(footer);
                }

                if (footer is IRibbonItem ri)
                {
                    ri.ApplyLayout(new RibbonItemLayout(RibbonItemSize.Medium, Metrics, false, true));
                }

                if (footer is FrameworkElement fe)
                {
                    fe.HorizontalAlignment = HorizontalAlignment.Stretch;
                    RibbonItemHelper.SetOwner(fe, RibbonItemHelper.FindOwner(this));
                }

                root.Children.Add(footer);
            }
        }

        // The popup is not part of the ribbon's tree: carry the effective theme over before resolving brushes.
        var chrome = new Border { Child = root, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(6), RequestedTheme = ActualTheme };
        RibbonTheme.SetThemeBrush(chrome, Border.BackgroundProperty, "RibbonPopupBackgroundBrush");
        RibbonTheme.SetThemeBrush(chrome, Border.BorderBrushProperty, "RibbonPopupBorderBrush");
        chrome.KeyDown += (_, e) =>
        {
            if (e.Key == Windows.System.VirtualKey.Escape)
            {
                CloseDropDown(restoreFocus: true);
                e.Handled = true;
            }
        };
        _popup = new Popup { Child = chrome, IsLightDismissEnabled = true, XamlRoot = XamlRoot };
        _popup.Closed += (_, _) =>
        {
            _previewEndPending = false;
            Preview(null);
            root.Children.Clear();
        };
        var anchor = IsInline ? (FrameworkElement?)_inlineRoot ?? this : (FrameworkElement?)_dropDownButton ?? this;
        RibbonPopupPlacement.PlaceBelow(_popup, anchor, overlapAnchor: IsInline, gap: 0);
        _popup.IsOpen = true;

        // Keyboard: start on the selected item (or the first one).
        var containers = Descendants(itemsHost).OfType<Button>().Where(b => b.Tag is not null).ToList();
        var initial = containers.FirstOrDefault(b => IsSelected(b.Tag)) ?? containers.FirstOrDefault();
        DispatcherQueue?.TryEnqueue(() =>
        {
            if (_popup is { IsOpen: true } && (initial?.Focus(FocusState.Programmatic) != true))
            {
                filter?.Focus(FocusState.Programmatic);
            }
        });
    }

    private string? GetCategory(object item)
        => CategorySelector?.Invoke(item) ?? item switch
        {
            RibbonGalleryItem g => g.Category,
            Model.RibbonGalleryItemModel m => m.Category,
            _ => null,
        };

    /// <summary>Closes the popup.</summary>
    public void CloseDropDown() => CloseDropDown(restoreFocus: false);

    private void CloseDropDown(bool restoreFocus)
    {
        if (_popup is not { IsOpen: true })
        {
            return;
        }

        // Keyboard users get a keyboard focus visual back on the opener; pointer users keep a quiet focus.
        var focusState = XamlRoot is { } xamlRoot && FocusManager.GetFocusedElement(xamlRoot) is Control { FocusState: FocusState.Keyboard }
            ? FocusState.Keyboard
            : FocusState.Programmatic;
        _popup.IsOpen = false;
        if (!restoreFocus)
        {
            return;
        }

        // Back to the More / drop-down button that opened the popup (or the selected inline item).
        var targets = new Control?[]
        {
            _opener,
            IsInline ? _moreButton : _dropDownButton,
            IsInline ? _inlinePanel.Children.OfType<Button>().FirstOrDefault(b => IsSelected(b.Tag)) ?? _inlinePanel.Children.OfType<Button>().FirstOrDefault() : null,
        };
        foreach (var target in targets)
        {
            if (target is { IsTabStop: true, IsEnabled: true } && target.Focus(focusState))
            {
                break;
            }
        }
    }

    /// <inheritdoc />
    protected override FrameworkElement? CreateLinkedCopyCore()
    {
        var copy = new RibbonGallery { IsDropDownOnly = true };
        RibbonItemHelper.LinkCommon(this, copy);
        foreach (var dp in new[] { ItemTemplateProperty, ItemWidthProperty, ItemHeightProperty, MaxColumnsProperty, MinColumnsProperty, DropDownColumnsProperty, IsFilterEnabledProperty, ShowItemLabelsProperty })
        {
            RibbonItemHelper.Link(this, copy, dp);
        }

        RibbonItemHelper.Link(this, copy, ItemsSourceProperty);
        if (ItemsSource is null)
        {
            copy.ItemsSource = Items;
        }

        RibbonItemHelper.Link(this, copy, SelectedItemProperty, twoWay: true);
        copy.CategorySelector = CategorySelector;
        copy.ItemClick += (_, e) =>
        {
            ItemClick?.Invoke(this, e);
            ExecuteCommand(e.Value);
        };
        copy.ItemPreview += (_, e) => Preview(e.Item);
        return copy;
    }

    /// <inheritdoc />
    protected override void OnUnlinkedCore()
    {
        base.OnUnlinkedCore();

        // Stop observing the source's collection (ItemsSource or its Items) and drop the containers.
        CloseDropDown();
        ItemsSource = null;
    }

    /// <inheritdoc />
    protected override IEnumerable<MenuFlyoutItemBase> CreateOverflowMenuItemsCore()
        => [RibbonItemHelper.CreateMenuItem(this, Label, Icon, OpenDropDown)];

    /// <inheritdoc />
    protected override RibbonKeyTipResult OnKeyTipCore()
    {
        OpenDropDown();
        return RibbonKeyTipResult.Close;
    }

    /// <inheritdoc />
    protected override bool InvokeCore()
    {
        OpenDropDown();
        return true;
    }
}

/// <summary>Arguments of gallery events.</summary>
public sealed class RibbonGalleryItemEventArgs(object? item, object? value) : EventArgs
{
    /// <summary>Item (container data).</summary>
    public object? Item { get; } = item;

    /// <summary>Value (RibbonGalleryItem.Value or the item).</summary>
    public object? Value { get; } = value;
}
