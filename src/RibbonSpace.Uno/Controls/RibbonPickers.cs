using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using RibbonSpace.Localization;
using RibbonSpace.Model;
using Windows.System;

namespace RibbonSpace.Controls;

/// <summary>Hover grid picker (Office "Insert Table"): the command receives a <see cref="RibbonGridSize"/>.</summary>
public partial class RibbonGridPicker : Control
{
    /// <summary>Identifies <see cref="Rows"/>.</summary>
    public static readonly DependencyProperty RowsProperty = DependencyProperty.Register(nameof(Rows), typeof(int), typeof(RibbonGridPicker), new PropertyMetadata(8, (d, _) => ((RibbonGridPicker)d).Build()));

    /// <summary>Identifies <see cref="Columns"/>.</summary>
    public static readonly DependencyProperty ColumnsProperty = DependencyProperty.Register(nameof(Columns), typeof(int), typeof(RibbonGridPicker), new PropertyMetadata(10, (d, _) => ((RibbonGridPicker)d).Build()));

    /// <summary>Identifies <see cref="Command"/>.</summary>
    public static readonly DependencyProperty CommandProperty = DependencyProperty.Register(nameof(Command), typeof(ICommand), typeof(RibbonGridPicker), new PropertyMetadata(null));

    private readonly Grid _grid = new() { RowSpacing = 2, ColumnSpacing = 2 };
    private readonly TextBlock _caption = new() { Margin = new Thickness(2, 0, 2, 6) };
    private readonly StackPanel _root = new() { Padding = new Thickness(4) };
    private RibbonGridSize _hover = new(0, 0);

    /// <summary>Creates a grid picker.</summary>
    public RibbonGridPicker()
    {
        DefaultStyleKey = typeof(RibbonGridPicker);
        RibbonTheme.EnsureResources();
        _root.Children.Add(_caption);
        _root.Children.Add(_grid);
        IsTabStop = true;
        UseSystemFocusVisuals = true;
        KeyDown += OnKeyDown;
        ActualThemeChanged += (_, _) => Highlight(_hover);
        Build();
    }

    /// <summary>
    /// Raised when a size is picked (click, Enter or Space), before the command executes; the hosting popup / flyout is
    /// closed afterwards.
    /// </summary>
    public event EventHandler<RibbonGridSize>? SizePicked;

    /// <summary>Rows.</summary>
    public int Rows { get => (int)GetValue(RowsProperty); set => SetValue(RowsProperty, value); }

    /// <summary>Columns.</summary>
    public int Columns { get => (int)GetValue(ColumnsProperty); set => SetValue(ColumnsProperty, value); }

    /// <summary>Command receiving the picked <see cref="RibbonGridSize"/>.</summary>
    public ICommand? Command { get => (ICommand?)GetValue(CommandProperty); set => SetValue(CommandProperty, value); }

    /// <summary>Currently highlighted size.</summary>
    public RibbonGridSize HighlightedSize => _hover;

    /// <inheritdoc />
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        if (GetTemplateChild("PART_Host") is Border host)
        {
            if (VisualTreeHelper.GetParent(_root) is Border old)
            {
                old.Child = null;
            }

            host.Child = _root;
        }
    }

    private void Build()
    {
        _grid.Children.Clear();
        _grid.RowDefinitions.Clear();
        _grid.ColumnDefinitions.Clear();
        for (var r = 0; r < Rows; r++)
        {
            _grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(16) });
        }

        for (var c = 0; c < Columns; c++)
        {
            _grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(16) });
        }

        for (var r = 0; r < Rows; r++)
        {
            for (var c = 0; c < Columns; c++)
            {
                var cell = new Border { BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(1), Tag = new RibbonGridSize(r + 1, c + 1), Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent) };
                Grid.SetRow(cell, r);
                Grid.SetColumn(cell, c);
                cell.PointerEntered += (s, _) => Highlight((RibbonGridSize)((FrameworkElement)s).Tag);
                cell.Tapped += (s, _) => Pick((RibbonGridSize)((FrameworkElement)s).Tag);
                _grid.Children.Add(cell);
            }
        }

        Highlight(new RibbonGridSize(0, 0));
    }

    /// <summary>Highlights a size.</summary>
    public void Highlight(RibbonGridSize size)
    {
        _hover = size;
        _caption.Text = size.Rows == 0 ? RibbonStrings.Current.InsertTable : RibbonStrings.Current.Format(nameof(RibbonStrings.TablePickerFormat), size.Columns, size.Rows);
        var on = RibbonTheme.GetBrush(this, "RibbonAccentSubtleStrongBrush");
        var onBorder = RibbonTheme.GetBrush(this, "RibbonAccentBrush");
        var off = RibbonTheme.GetBrush(this, "RibbonInputBorderBrush");
        foreach (var cell in _grid.Children.OfType<Border>())
        {
            var s = (RibbonGridSize)cell.Tag;
            var active = s.Rows <= size.Rows && s.Columns <= size.Columns;
            cell.Background = active ? on : new SolidColorBrush(Microsoft.UI.Colors.Transparent);
            cell.BorderBrush = active ? onBorder : off;
        }

        AutomationProperties.SetName(this, _caption.Text);
    }

    /// <summary>Picks a size.</summary>
    public void Pick(RibbonGridSize size)
    {
        // Resolved before closing: the owner is found through the popup / flyout this picker lives in.
        var owner = RibbonItemHelper.FindOwner(this);
        SizePicked?.Invoke(this, size);
        if (Command is { } command && command.CanExecute(size))
        {
            command.Execute(size);
        }

        owner?.OnItemInvoked(this, null, size);
        CloseHostPopup();
        Highlight(new RibbonGridSize(0, 0));
    }

    private void CloseHostPopup()
    {
        if (XamlRoot is null)
        {
            return;
        }

        foreach (var popup in VisualTreeHelper.GetOpenPopupsForXamlRoot(XamlRoot))
        {
            if (popup.Child is DependencyObject child && IsWithin(child))
            {
                popup.IsOpen = false;
                return;
            }
        }
    }

    private bool IsWithin(DependencyObject ancestor)
    {
        DependencyObject? current = this;
        while (current is not null)
        {
            if (ReferenceEquals(current, ancestor))
            {
                return true;
            }

            current = VisualTreeHelper.GetParent(current) ?? (current as FrameworkElement)?.Parent;
        }

        return false;
    }

    /// <inheritdoc />
    protected override void OnGotFocus(RoutedEventArgs e)
    {
        base.OnGotFocus(e);
        if (FocusState == FocusState.Keyboard && _hover.Rows == 0)
        {
            Highlight(new RibbonGridSize(1, 1));
        }
    }

    private void OnKeyDown(object sender, KeyRoutedEventArgs e)
    {
        var s = _hover.Rows == 0 ? new RibbonGridSize(1, 1) : _hover;
        switch (e.Key)
        {
            case VirtualKey.Right: s = s with { Columns = Math.Min(Columns, s.Columns + 1) }; break;
            case VirtualKey.Left: s = s with { Columns = Math.Max(1, s.Columns - 1) }; break;
            case VirtualKey.Down: s = s with { Rows = Math.Min(Rows, s.Rows + 1) }; break;
            case VirtualKey.Up: s = s with { Rows = Math.Max(1, s.Rows - 1) }; break;
            case VirtualKey.Home: s = new RibbonGridSize(1, 1); break;
            case VirtualKey.Enter:
            case VirtualKey.Space:
                Pick(s);
                e.Handled = true;
                return;
            default: return;
        }

        Highlight(s);
        e.Handled = true;
    }
}

/// <summary>Segment of a <see cref="RibbonSegmentedControl"/>.</summary>
public partial class RibbonSegment : DependencyObject
{
    /// <summary>Identifies <see cref="Label"/>.</summary>
    public static readonly DependencyProperty LabelProperty = DependencyProperty.Register(nameof(Label), typeof(string), typeof(RibbonSegment), new PropertyMetadata(null));

    /// <summary>Identifies <see cref="Icon"/>.</summary>
    public static readonly DependencyProperty IconProperty = DependencyProperty.Register(nameof(Icon), typeof(object), typeof(RibbonSegment), new PropertyMetadata(null));

    /// <summary>Identifies <see cref="Value"/>.</summary>
    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(nameof(Value), typeof(object), typeof(RibbonSegment), new PropertyMetadata(null));

    /// <summary>Label.</summary>
    public string? Label { get => (string?)GetValue(LabelProperty); set => SetValue(LabelProperty, value); }

    /// <summary>Icon.</summary>
    public object? Icon { get => GetValue(IconProperty); set => SetValue(IconProperty, value); }

    /// <summary>Value.</summary>
    public object? Value { get => GetValue(ValueProperty); set => SetValue(ValueProperty, value); }
}

/// <summary>Segmented control / workspace switcher (single selection, arrow-key navigation).</summary>
[ContentProperty(Name = nameof(Segments))]
public partial class RibbonSegmentedControl : RibbonControlBase
{
    /// <summary>Identifies <see cref="SelectedIndex"/>.</summary>
    public static readonly DependencyProperty SelectedIndexProperty = DependencyProperty.Register(nameof(SelectedIndex), typeof(int), typeof(RibbonSegmentedControl), new PropertyMetadata(0, (d, e) => ((RibbonSegmentedControl)d).OnSelectedIndexChanged((int)e.NewValue)));

    private StackPanel? _host;

    /// <summary>Creates a segmented control.</summary>
    public RibbonSegmentedControl()
    {
        DefaultStyleKey = typeof(RibbonSegmentedControl);
        Segments.CollectionChanged += (_, _) => Build();
        CanAddToQuickAccess = false;
        Size = RibbonItemSize.Medium;
        KeyDown += OnKeyDown;
        IsTabStop = true;
        UseSystemFocusVisuals = true;
    }

    /// <summary>Raised when the selection changes (user or programmatic); the command only runs for user selections.</summary>
    public event EventHandler<RibbonSegment?>? SelectionChanged;

    /// <summary>Segments.</summary>
    public ObservableCollection<RibbonSegment> Segments { get; } = [];

    /// <summary>Selected index.</summary>
    public int SelectedIndex { get => (int)GetValue(SelectedIndexProperty); set => SetValue(SelectedIndexProperty, value); }

    /// <summary>Selected segment.</summary>
    public RibbonSegment? SelectedSegment => SelectedIndex >= 0 && SelectedIndex < Segments.Count ? Segments[SelectedIndex] : null;

    /// <inheritdoc />
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _host = GetTemplateChild("PART_ItemsHost") as StackPanel;
        Build();
    }

    /// <inheritdoc />
    protected override void OnLayoutAppliedCore(RibbonItemLayout layout) => Build();

    private void Build()
    {
        if (_host is null)
        {
            return;
        }

        _host.Children.Clear();
        for (var i = 0; i < Segments.Count; i++)
        {
            var segment = Segments[i];
            var index = i;
            var button = new RibbonToggleButton
            {
                Label = segment.Label,
                Icon = segment.Icon,
                IsChecked = i == SelectedIndex,
                CanAddToQuickAccess = false,
                IsTabStop = false,
            };
            button.ApplyLayout(new RibbonItemLayout(RibbonItemSize.Medium, Metrics, IsSimplified, segment.Label is not null));
            button.Click += (_, _) =>
            {
                SelectFromUser(index);
                button.IsChecked = true;
            };
            AutomationProperties.SetName(button, segment.Label ?? string.Empty);
            _host.Children.Add(button);
        }
    }

    private void OnSelectedIndexChanged(int index)
    {
        if (_host is not null)
        {
            for (var i = 0; i < _host.Children.Count; i++)
            {
                if (_host.Children[i] is RibbonToggleButton toggle)
                {
                    toggle.IsChecked = i == index;
                }
            }
        }

        SelectionChanged?.Invoke(this, SelectedSegment);
    }

    /// <summary>Selects a segment on behalf of the user (click, keys, overflow menu) and executes the command.</summary>
    private void SelectFromUser(int index)
    {
        if (index < 0 || index >= Segments.Count)
        {
            return;
        }

        SelectedIndex = index;
        if (SelectedSegment is { } segment)
        {
            ExecuteCommand(segment.Value ?? segment.Label);
        }
    }

    private void OnKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (Segments.Count == 0)
        {
            return;
        }

        var current = Math.Clamp(SelectedIndex, 0, Segments.Count - 1);
        switch (e.Key)
        {
            case VirtualKey.Left: SelectFromUser(Math.Max(0, current - 1)); e.Handled = true; break;
            case VirtualKey.Right: SelectFromUser(Math.Min(Segments.Count - 1, current + 1)); e.Handled = true; break;
            case VirtualKey.Home: SelectFromUser(0); e.Handled = true; break;
            case VirtualKey.End: SelectFromUser(Segments.Count - 1); e.Handled = true; break;
            case VirtualKey.Enter:
            case VirtualKey.Space: SelectFromUser(current); e.Handled = true; break;
        }
    }

    /// <inheritdoc />
    protected override IEnumerable<MenuFlyoutItemBase> CreateOverflowMenuItemsCore()
        => Segments.Select((s, i) =>
        {
            var item = new RadioMenuFlyoutItem { Text = s.Label ?? string.Empty, GroupName = GetHashCode().ToString(System.Globalization.CultureInfo.InvariantCulture), IsChecked = i == SelectedIndex };
            item.Click += (_, _) => SelectFromUser(i);
            return (MenuFlyoutItemBase)item;
        });

    /// <inheritdoc />
    protected override bool InvokeCore()
    {
        if (Segments.Count == 0)
        {
            return false;
        }

        SelectFromUser((Math.Max(0, SelectedIndex) + 1) % Segments.Count);
        return true;
    }
}
