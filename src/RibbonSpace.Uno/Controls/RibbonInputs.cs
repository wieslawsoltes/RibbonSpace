using System.Collections;
using System.Collections.ObjectModel;
using System.Globalization;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.System;

namespace RibbonSpace.Controls;

/// <summary>Shared helpers for input items (label + input box layouts).</summary>
public abstract partial class RibbonInputBase : RibbonControlBase
{
    /// <summary>Identifies <see cref="InputWidth"/>.</summary>
    public static readonly DependencyProperty InputWidthProperty = DependencyProperty.Register(nameof(InputWidth), typeof(double), typeof(RibbonInputBase), new PropertyMetadata(120d, (d, _) => RibbonItemHelper.InvalidateHostLayout((FrameworkElement)d)));

    /// <summary>Identifies <see cref="PlaceholderText"/>.</summary>
    public static readonly DependencyProperty PlaceholderTextProperty = DependencyProperty.Register(nameof(PlaceholderText), typeof(string), typeof(RibbonInputBase), new PropertyMetadata(null));

    /// <summary>Identifies <see cref="InputHeight"/>.</summary>
    public static readonly DependencyProperty InputHeightProperty = DependencyProperty.Register(nameof(InputHeight), typeof(double), typeof(RibbonInputBase), new PropertyMetadata(22d));

    /// <summary>Identifies <see cref="LabelVisibility"/>.</summary>
    public static readonly DependencyProperty LabelVisibilityProperty = DependencyProperty.Register(nameof(LabelVisibility), typeof(Visibility), typeof(RibbonInputBase), new PropertyMetadata(Visibility.Collapsed));

    /// <summary>Identifies <see cref="IconVisibility"/>.</summary>
    public static readonly DependencyProperty IconVisibilityProperty = DependencyProperty.Register(nameof(IconVisibility), typeof(Visibility), typeof(RibbonInputBase), new PropertyMetadata(Visibility.Collapsed));

    private bool _isPointerOver;
    private bool _hasFocus;

    /// <summary>Creates the input.</summary>
    protected RibbonInputBase()
    {
        Size = RibbonItemSize.Medium;
        IsTabStop = false;
        CanAddToQuickAccess = true;
        IsEnabledChanged += (_, _) => UpdateInputStates(true);
    }

    /// <summary>Width of the input box.</summary>
    public double InputWidth { get => (double)GetValue(InputWidthProperty); set => SetValue(InputWidthProperty, value); }

    /// <summary>Placeholder.</summary>
    public string? PlaceholderText { get => (string?)GetValue(PlaceholderTextProperty); set => SetValue(PlaceholderTextProperty, value); }

    /// <summary>Height of the input box (template use).</summary>
    public double InputHeight { get => (double)GetValue(InputHeightProperty); private set => SetValue(InputHeightProperty, value); }

    /// <summary>Label visibility (template use).</summary>
    public Visibility LabelVisibility { get => (Visibility)GetValue(LabelVisibilityProperty); private set => SetValue(LabelVisibilityProperty, value); }

    /// <summary>Icon visibility (template use).</summary>
    public Visibility IconVisibility { get => (Visibility)GetValue(IconVisibilityProperty); private set => SetValue(IconVisibilityProperty, value); }

    /// <inheritdoc />
    protected override bool UsesCommandCanExecute => false;

    /// <inheritdoc />
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        UpdateInputLayout();
        UpdateInputStates(false);
    }

    /// <inheritdoc />
    protected override void OnLayoutAppliedCore(RibbonItemLayout layout) => UpdateInputLayout();

    /// <inheritdoc />
    protected override void OnRibbonPropertyChanged(DependencyPropertyChangedEventArgs e) => UpdateInputLayout();

    private void UpdateInputLayout()
    {
        InputHeight = IsSimplified ? Math.Max(24, Metrics.SimplifiedItemHeight - 4) : Metrics.RowHeight;
        LabelVisibility = !string.IsNullOrEmpty(Label) && ShowsLabelByDefault && (IsSimplified || (ActualShowLabel && CurrentSize != RibbonItemSize.Small)) ? Visibility.Visible : Visibility.Collapsed;
        IconVisibility = Icon is not null && (CurrentSize != RibbonItemSize.Small || LabelVisibility == Visibility.Collapsed) ? Visibility.Visible : Visibility.Collapsed;
    }

    /// <inheritdoc />
    protected override void OnGotFocus(RoutedEventArgs e)
    {
        base.OnGotFocus(e);
        _hasFocus = true;
        UpdateInputStates(true);
    }

    /// <inheritdoc />
    protected override void OnLostFocus(RoutedEventArgs e)
    {
        base.OnLostFocus(e);
        _hasFocus = false;
        UpdateInputStates(true);
    }

    /// <inheritdoc />
    protected override void OnPointerEntered(PointerRoutedEventArgs e)
    {
        base.OnPointerEntered(e);
        _isPointerOver = true;
        UpdateInputStates(true);
    }

    /// <inheritdoc />
    protected override void OnPointerExited(PointerRoutedEventArgs e)
    {
        base.OnPointerExited(e);
        _isPointerOver = false;
        UpdateInputStates(true);
    }

    /// <summary>
    /// Moves to InputDisabled, InputFocused, InputPointerOver or InputNormal (one state group, so the focus border survives
    /// the pointer leaving and disabled inputs never highlight).
    /// </summary>
    protected void UpdateInputStates(bool useTransitions)
    {
        var state = !IsEnabled ? "InputDisabled" : _hasFocus ? "InputFocused" : _isPointerOver ? "InputPointerOver" : "InputNormal";
        VisualStateManager.GoToState(this, state, useTransitions);
    }

    /// <summary>Whether the label is shown next to the input (spinners: yes, combo boxes: only when requested).</summary>
    protected virtual bool ShowsLabelByDefault => true;

    /// <inheritdoc />
    protected override IEnumerable<MenuFlyoutItemBase> CreateOverflowMenuItemsCore() => [];

    /// <summary>Parses a double using the current culture, then the invariant culture.</summary>
    protected static bool TryParseNumber(string? text, out double value)
        => double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value)
        || double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
}

/// <summary>Editable / read-only ribbon combo box (font family, font size, number format, zoom).</summary>
public partial class RibbonComboBox : RibbonInputBase
{
    /// <summary>Identifies <see cref="ItemsSource"/>.</summary>
    public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(nameof(ItemsSource), typeof(object), typeof(RibbonComboBox), new PropertyMetadata(null, (d, _) => ((RibbonComboBox)d).OnItemsSourceChanged()));

    /// <summary>Identifies <see cref="SelectedItem"/>.</summary>
    public static readonly DependencyProperty SelectedItemProperty = DependencyProperty.Register(nameof(SelectedItem), typeof(object), typeof(RibbonComboBox), new PropertyMetadata(null, (d, e) => ((RibbonComboBox)d).OnSelectedItemChanged(e.OldValue, e.NewValue)));

    /// <summary>Identifies <see cref="Text"/>.</summary>
    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(nameof(Text), typeof(string), typeof(RibbonComboBox), new PropertyMetadata(null, (d, _) => ((RibbonComboBox)d).OnTextChanged()));

    /// <summary>Identifies <see cref="IsEditable"/>.</summary>
    public static readonly DependencyProperty IsEditableProperty = DependencyProperty.Register(nameof(IsEditable), typeof(bool), typeof(RibbonComboBox), new PropertyMetadata(false, (d, _) => ((RibbonComboBox)d).UpdateEditable()));

    /// <summary>Identifies <see cref="ItemTemplate"/>.</summary>
    public static readonly DependencyProperty ItemTemplateProperty = DependencyProperty.Register(nameof(ItemTemplate), typeof(DataTemplate), typeof(RibbonComboBox), new PropertyMetadata(null));

    /// <summary>Identifies <see cref="DisplayMemberPath"/>.</summary>
    public static readonly DependencyProperty DisplayMemberPathProperty = DependencyProperty.Register(nameof(DisplayMemberPath), typeof(string), typeof(RibbonComboBox), new PropertyMetadata(null, (d, _) => ((RibbonComboBox)d).OnDisplayMemberPathChanged()));

    /// <summary>Identifies <see cref="MaxDropDownHeight"/>.</summary>
    public static readonly DependencyProperty MaxDropDownHeightProperty = DependencyProperty.Register(nameof(MaxDropDownHeight), typeof(double), typeof(RibbonComboBox), new PropertyMetadata(420d));

    /// <summary>Identifies <see cref="IsFontPreview"/>.</summary>
    public static readonly DependencyProperty IsFontPreviewProperty = DependencyProperty.Register(nameof(IsFontPreview), typeof(bool), typeof(RibbonComboBox), new PropertyMetadata(false));

    private TextBox? _textBox;
    private Button? _dropDownButton;
    private Popup? _popup;
    private StackPanel? _list;
    private ScrollViewer? _scroll;
    private int _highlight = -1;
    private bool _syncing;

    /// <summary>Creates a combo box.</summary>
    public RibbonComboBox()
    {
        DefaultStyleKey = typeof(RibbonComboBox);
        Size = RibbonItemSize.Small;
        ShowLabel = false;
    }

    /// <summary>Raised after the user picked an item or committed text.</summary>
    public event EventHandler<RibbonComboBoxCommittedEventArgs>? Committed;

    /// <summary>Raised when the selected item changes.</summary>
    public event EventHandler<SelectionChangedEventArgs>? SelectionChanged;

    /// <summary>Items (in addition to <see cref="ItemsSource"/>).</summary>
    public ObservableCollection<object> Items { get; } = [];

    /// <summary>Items source (overrides <see cref="Items"/>).</summary>
    public object? ItemsSource { get => GetValue(ItemsSourceProperty); set => SetValue(ItemsSourceProperty, value); }

    /// <summary>Selected item.</summary>
    public object? SelectedItem { get => GetValue(SelectedItemProperty); set => SetValue(SelectedItemProperty, value); }

    /// <summary>Text of the input.</summary>
    public string? Text { get => (string?)GetValue(TextProperty); set => SetValue(TextProperty, value); }

    /// <summary>Allows typing arbitrary values.</summary>
    public bool IsEditable { get => (bool)GetValue(IsEditableProperty); set => SetValue(IsEditableProperty, value); }

    /// <summary>Item template of the drop-down.</summary>
    public DataTemplate? ItemTemplate { get => (DataTemplate?)GetValue(ItemTemplateProperty); set => SetValue(ItemTemplateProperty, value); }

    /// <summary>Maximum drop-down height.</summary>
    public double MaxDropDownHeight { get => (double)GetValue(MaxDropDownHeightProperty); set => SetValue(MaxDropDownHeightProperty, value); }

    /// <summary>Renders each entry in its own font (font pickers).</summary>
    public bool IsFontPreview { get => (bool)GetValue(IsFontPreviewProperty); set => SetValue(IsFontPreviewProperty, value); }

    /// <summary>Converts items to text (defaults to <see cref="DisplayMemberPath"/>, then ToString / RibbonNodeModel label).</summary>
    /// <remarks>Preferred over <see cref="DisplayMemberPath"/> in trimmed / AOT apps: it needs no binding metadata.</remarks>
    public Func<object?, string>? ItemTextSelector { get; set; }

    /// <summary>Property path of the item shown as its text (resolved with a data binding, like WinUI's ComboBox).</summary>
    public string? DisplayMemberPath { get => (string?)GetValue(DisplayMemberPathProperty); set => SetValue(DisplayMemberPathProperty, value); }

    private TextBlock? _memberEvaluator;

    private void OnDisplayMemberPathChanged()
    {
        if (SelectedItem is { } selected)
        {
            Text = GetItemText(selected);
        }

        if (IsDropDownOpen)
        {
            BuildList();
        }
    }

    private string? EvaluateDisplayMember(object item)
    {
        if (string.IsNullOrEmpty(DisplayMemberPath) || item is string)
        {
            return null;
        }

        // A detached TextBlock evaluates the path through the platform binding engine (no reflection here).
        _memberEvaluator ??= new TextBlock();
        _memberEvaluator.SetBinding(TextBlock.TextProperty, new Microsoft.UI.Xaml.Data.Binding { Source = item, Path = new PropertyPath(DisplayMemberPath), Mode = Microsoft.UI.Xaml.Data.BindingMode.OneTime });
        var text = _memberEvaluator.Text;
        _memberEvaluator.ClearValue(TextBlock.TextProperty);
        return text;
    }

    /// <summary>True while the drop-down is open.</summary>
    public bool IsDropDownOpen => _popup?.IsOpen == true;

    /// <inheritdoc />
    protected override bool ShowsLabelByDefault => ShowLabel;

    /// <summary>Effective items.</summary>
    public IReadOnlyList<object> EffectiveItems => ItemsSource is IEnumerable e and not string ? e.Cast<object>().ToArray() : Items.ToArray();

    /// <summary>Text for an item.</summary>
    public string GetItemText(object? item)
        => ItemTextSelector?.Invoke(item) ?? (item is null ? null : EvaluateDisplayMember(item)) ?? item switch
        {
            null => string.Empty,
            Model.RibbonNodeModel node => node.Label ?? node.Id,
            RibbonGalleryItem g => g.Label ?? string.Empty,
            _ => Convert.ToString(item, CultureInfo.CurrentCulture) ?? string.Empty,
        };

    /// <inheritdoc />
    protected override void OnApplyTemplate()
    {
        if (_textBox is not null)
        {
            _textBox.KeyDown -= OnTextKeyDown;
            _textBox.LostFocus -= OnTextLostFocus;
            _textBox.GotFocus -= OnTextGotFocus;
        }

        if (_dropDownButton is not null)
        {
            _dropDownButton.Click -= OnDropDownClick;
        }

        base.OnApplyTemplate();
        _textBox = GetTemplateChild("PART_TextBox") as TextBox;
        _dropDownButton = GetTemplateChild("PART_DropDownButton") as Button;
        if (_textBox is not null)
        {
            _textBox.KeyDown += OnTextKeyDown;
            _textBox.LostFocus += OnTextLostFocus;
            _textBox.GotFocus += OnTextGotFocus;
            _textBox.Text = Text ?? string.Empty;
            Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(_textBox, Label ?? string.Empty);
        }

        if (_dropDownButton is not null)
        {
            _dropDownButton.Click += OnDropDownClick;
        }

        UpdateEditable();
    }

    private void UpdateEditable()
    {
        if (_textBox is not null)
        {
            _textBox.IsReadOnly = !IsEditable;
        }
    }

    private void OnItemsSourceChanged()
    {
        if (IsDropDownOpen)
        {
            BuildList();
        }
    }

    private void OnSelectedItemChanged(object? oldValue, object? newValue)
    {
        if (!_syncing)
        {
            _syncing = true;
            Text = newValue is null ? Text : GetItemText(newValue);
            _syncing = false;
        }

        SelectionChanged?.Invoke(this, new SelectionChangedEventArgs(oldValue is null ? [] : [oldValue], newValue is null ? [] : [newValue]));
    }

    private void OnTextChanged()
    {
        if (_textBox is not null && _textBox.Text != (Text ?? string.Empty))
        {
            _textBox.Text = Text ?? string.Empty;
        }
    }

    private void OnTextGotFocus(object sender, RoutedEventArgs e) => _textBox?.SelectAll();

    private void OnTextLostFocus(object sender, RoutedEventArgs e)
    {
        if (IsEditable && _textBox is not null && _textBox.Text != (Text ?? string.Empty) && !IsDropDownOpen)
        {
            CommitText(_textBox.Text);
        }
    }

    private void OnDropDownClick(object sender, RoutedEventArgs e)
    {
        if (IsDropDownOpen)
        {
            CloseDropDown();
        }
        else
        {
            DispatcherQueue?.TryEnqueue(OpenDropDown);
        }
    }

    private void OnTextKeyDown(object sender, KeyRoutedEventArgs e)
    {
        var items = EffectiveItems;
        switch (e.Key)
        {
            case VirtualKey.Enter:
                if (IsDropDownOpen && _highlight >= 0 && _highlight < items.Count)
                {
                    Commit(items[_highlight]);
                }
                else if (IsEditable && _textBox is not null)
                {
                    CommitText(_textBox.Text);
                }

                e.Handled = true;
                break;
            case VirtualKey.Escape:
                if (IsDropDownOpen)
                {
                    CloseDropDown();
                    e.Handled = true;
                }
                else if (_textBox is not null)
                {
                    _textBox.Text = Text ?? string.Empty;
                }

                break;
            case VirtualKey.F4:
                OpenDropDown();
                e.Handled = true;
                break;
            case VirtualKey.Down:
            case VirtualKey.Up:
                if (items.Count == 0)
                {
                    break;
                }

                if (Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Menu).HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down))
                {
                    OpenDropDown();
                    e.Handled = true;
                    break;
                }

                var current = IsDropDownOpen ? _highlight : IndexOf(items, SelectedItem);
                var next = Math.Clamp(current + (e.Key == VirtualKey.Down ? 1 : -1), 0, items.Count - 1);
                if (IsDropDownOpen)
                {
                    Highlight(next);
                }
                else
                {
                    Commit(items[next]);
                }

                e.Handled = true;
                break;
        }
    }

    private static int IndexOf(IReadOnlyList<object> items, object? item)
    {
        for (var i = 0; i < items.Count; i++)
        {
            if (Equals(items[i], item))
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>Commits an item (selects it, executes the command, raises Committed).</summary>
    public void Commit(object? item)
    {
        CloseDropDown();
        SelectedItem = item;
        _syncing = true;
        Text = GetItemText(item);
        _syncing = false;
        if (_textBox is not null)
        {
            _textBox.Text = Text ?? string.Empty;
        }

        Committed?.Invoke(this, new RibbonComboBoxCommittedEventArgs(item, Text));
        ExecuteCommand(item);
    }

    /// <summary>Commits free text (editable combo boxes). Matching items are selected.</summary>
    public void CommitText(string? text)
    {
        var match = EffectiveItems.FirstOrDefault(i => string.Equals(GetItemText(i), text, StringComparison.CurrentCultureIgnoreCase));
        if (match is not null)
        {
            Commit(match);
            return;
        }

        _syncing = true;
        Text = text;
        SelectedItem = null;
        _syncing = false;
        Committed?.Invoke(this, new RibbonComboBoxCommittedEventArgs(null, text));
        ExecuteCommand(text);
    }

    /// <summary>Opens the drop-down.</summary>
    public void OpenDropDown()
    {
        if (XamlRoot is null || !IsEnabled)
        {
            return;
        }

        if (_popup is null)
        {
            _list = new StackPanel { Padding = new Thickness(2) };
            _scroll = new ScrollViewer { Content = _list, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
            var chrome = new Border { Child = _scroll, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(6), Padding = new Thickness(0, 2, 0, 2), RequestedTheme = ActualTheme };

            // Re-resolved when RequestedTheme is updated on open (the popup is outside the ribbon's tree).
            RibbonTheme.SetThemeBrush(chrome, Border.BackgroundProperty, "RibbonPopupBackgroundBrush");
            RibbonTheme.SetThemeBrush(chrome, Border.BorderBrushProperty, "RibbonPopupBorderBrush");
            _popup = new Popup { Child = chrome, IsLightDismissEnabled = true };
            _popup.Closed += (_, _) => _highlight = -1;
        }

        BuildList();
        var anchor = (FrameworkElement?)GetTemplateChild("PART_InputBorder") ?? this;
        _popup.XamlRoot = XamlRoot;
        if (_popup.Child is FrameworkElement child)
        {
            child.MinWidth = anchor.ActualWidth;
            child.MaxHeight = MaxDropDownHeight;
            child.RequestedTheme = ActualTheme;
        }

        Primitives.RibbonPopupPlacement.PlaceBelow(_popup, anchor, gap: 1);
        _popup.IsOpen = true;
        Highlight(IndexOf(EffectiveItems, SelectedItem));
        _textBox?.Focus(FocusState.Programmatic);
    }

    /// <summary>Closes the drop-down.</summary>
    public void CloseDropDown()
    {
        if (_popup is { IsOpen: true })
        {
            _popup.IsOpen = false;
        }
    }

    private void BuildList()
    {
        if (_list is null)
        {
            return;
        }

        _list.Children.Clear();
        var items = EffectiveItems;
        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            object content;
            if (ItemTemplate is not null)
            {
                content = new ContentPresenter { Content = item, ContentTemplate = ItemTemplate };
            }
            else
            {
                var text = new TextBlock { Text = GetItemText(item), VerticalAlignment = VerticalAlignment.Center };
                if (IsFontPreview && text.Text.Length > 0)
                {
                    text.FontFamily = new FontFamily(text.Text);
                    text.FontSize = 14;
                }

                content = text;
            }

            var button = new Button { Content = content, Tag = item, HorizontalAlignment = HorizontalAlignment.Stretch, HorizontalContentAlignment = HorizontalAlignment.Left };
            if (Application.Current.Resources.TryGetValue("RibbonMenuItemButtonStyle", out var style))
            {
                button.Style = (Style)style;
            }

            var captured = item;
            button.Click += (_, _) => Commit(captured);
            _list.Children.Add(button);
        }
    }

    private void Highlight(int index)
    {
        _highlight = index;
        if (_list is null)
        {
            return;
        }

        for (var i = 0; i < _list.Children.Count; i++)
        {
            if (_list.Children[i] is Button button)
            {
                VisualStateManager.GoToState(button, i == index ? "Highlighted" : "NotHighlighted", false);
                button.Background = i == index ? RibbonTheme.GetBrush(button, "RibbonItemHoverBrush") : new SolidColorBrush(Microsoft.UI.Colors.Transparent);
                if (i == index)
                {
                    button.StartBringIntoView();
                }
            }
        }
    }

    /// <inheritdoc />
    protected override FrameworkElement? CreateLinkedCopyCore()
    {
        var copy = CreateCopyInstance();
        RibbonItemHelper.LinkCommon(this, copy);
        RibbonItemHelper.Link(this, copy, ItemsSourceProperty);
        RibbonItemHelper.Link(this, copy, SelectedItemProperty, twoWay: true);
        RibbonItemHelper.Link(this, copy, TextProperty, twoWay: true);
        RibbonItemHelper.Link(this, copy, IsEditableProperty);
        RibbonItemHelper.Link(this, copy, InputWidthProperty);
        RibbonItemHelper.Link(this, copy, ItemTemplateProperty);
        RibbonItemHelper.Link(this, copy, DisplayMemberPathProperty);
        RibbonItemHelper.Link(this, copy, IsFontPreviewProperty);
        if (ItemsSource is null)
        {
            copy.ItemsSource = Items;
        }

        copy.ItemTextSelector = ItemTextSelector;
        copy.Committed += (_, e) =>
        {
            Committed?.Invoke(this, e);
            ExecuteCommand(e.Item ?? e.Text);
        };
        return copy;
    }

    /// <inheritdoc />
    protected override void OnUnlinkedCore()
    {
        base.OnUnlinkedCore();
        CloseDropDown();
        ItemsSource = null;
        ItemTextSelector = null;
    }

    /// <summary>Creates the instance used for linked copies.</summary>
    protected virtual RibbonComboBox CreateCopyInstance() => this switch
    {
        RibbonFontComboBox => new RibbonFontComboBox(),
        RibbonFontSizeComboBox => new RibbonFontSizeComboBox(),
        _ => new RibbonComboBox(),
    };

    /// <inheritdoc />
    protected override RibbonKeyTipResult OnKeyTipCore()
    {
        _textBox?.Focus(FocusState.Keyboard);
        if (!IsEditable)
        {
            OpenDropDown();
        }

        return RibbonKeyTipResult.Close;
    }

    /// <inheritdoc />
    protected override bool InvokeCore()
    {
        OpenDropDown();
        return true;
    }
}

/// <summary>Arguments of <see cref="RibbonComboBox.Committed"/>.</summary>
public sealed class RibbonComboBoxCommittedEventArgs(object? item, string? text) : EventArgs
{
    /// <summary>Picked item (null for free text).</summary>
    public object? Item { get; } = item;

    /// <summary>Committed text.</summary>
    public string? Text { get; } = text;
}

/// <summary>Font family picker with preview (Office "Font").</summary>
public partial class RibbonFontComboBox : RibbonComboBox
{
    /// <summary>Common cross-platform font families.</summary>
    public static IReadOnlyList<string> DefaultFonts { get; } =
    [
        "Aptos", "Arial", "Arial Black", "Bahnschrift", "Calibri", "Cambria", "Candara", "Comic Sans MS", "Consolas", "Constantia", "Corbel",
        "Courier New", "Franklin Gothic", "Garamond", "Georgia", "Gill Sans", "Helvetica", "Impact", "Inter", "Lucida Console", "Menlo",
        "Monaco", "Open Sans", "Palatino", "Roboto", "Segoe UI", "Tahoma", "Times New Roman", "Trebuchet MS", "Verdana",
    ];

    /// <summary>Creates a font picker.</summary>
    public RibbonFontComboBox()
    {
        IsEditable = true;
        IsFontPreview = true;
        InputWidth = 132;
        Label = Localization.RibbonStrings.Current["Font"] == "Font" ? "Font" : Localization.RibbonStrings.Current["Font"];
        foreach (var font in DefaultFonts)
        {
            Items.Add(font);
        }
    }
}

/// <summary>Font size picker (Office "Font Size").</summary>
public partial class RibbonFontSizeComboBox : RibbonComboBox
{
    /// <summary>Office font sizes.</summary>
    public static IReadOnlyList<double> DefaultSizes { get; } = [8, 9, 10, 10.5, 11, 12, 14, 16, 18, 20, 22, 24, 26, 28, 36, 48, 72];

    /// <summary>Creates a font size picker.</summary>
    public RibbonFontSizeComboBox()
    {
        IsEditable = true;
        InputWidth = 46;
        Label = "Font Size";
        foreach (var size in DefaultSizes)
        {
            Items.Add(size);
        }
    }
}

/// <summary>Numeric spinner with units, arrow keys, wheel and label scrubbing.</summary>
public partial class RibbonSpinner : RibbonInputBase
{
    /// <summary>Identifies <see cref="Value"/>.</summary>
    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(nameof(Value), typeof(double), typeof(RibbonSpinner), new PropertyMetadata(0d, (d, e) => ((RibbonSpinner)d).OnValueChanged((double)e.OldValue, (double)e.NewValue)));

    /// <summary>Identifies <see cref="Minimum"/>.</summary>
    public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register(nameof(Minimum), typeof(double), typeof(RibbonSpinner), new PropertyMetadata(double.MinValue, (d, _) => ((RibbonSpinner)d).CoerceValue()));

    /// <summary>Identifies <see cref="Maximum"/>.</summary>
    public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(nameof(Maximum), typeof(double), typeof(RibbonSpinner), new PropertyMetadata(double.MaxValue, (d, _) => ((RibbonSpinner)d).CoerceValue()));

    /// <summary>Identifies <see cref="Increment"/>.</summary>
    public static readonly DependencyProperty IncrementProperty = DependencyProperty.Register(nameof(Increment), typeof(double), typeof(RibbonSpinner), new PropertyMetadata(1d));

    /// <summary>Identifies <see cref="Format"/>.</summary>
    public static readonly DependencyProperty FormatProperty = DependencyProperty.Register(nameof(Format), typeof(string), typeof(RibbonSpinner), new PropertyMetadata("0.##", (d, _) => ((RibbonSpinner)d).UpdateText()));

    /// <summary>Identifies <see cref="Unit"/>.</summary>
    public static readonly DependencyProperty UnitProperty = DependencyProperty.Register(nameof(Unit), typeof(string), typeof(RibbonSpinner), new PropertyMetadata(null, (d, _) => ((RibbonSpinner)d).UpdateText()));

    /// <summary>Identifies <see cref="IsScrubEnabled"/>.</summary>
    public static readonly DependencyProperty IsScrubEnabledProperty = DependencyProperty.Register(nameof(IsScrubEnabled), typeof(bool), typeof(RibbonSpinner), new PropertyMetadata(true));

    private TextBox? _textBox;
    private RepeatButton? _up;
    private RepeatButton? _down;
    private FrameworkElement? _scrubHandle;
    private Point? _scrubStart;
    private double _scrubValue;
    private double _scrubOldValue;

    /// <summary>Creates a spinner.</summary>
    public RibbonSpinner()
    {
        DefaultStyleKey = typeof(RibbonSpinner);
        InputWidth = 64;
    }

    /// <summary>Raised when the value changes (user or programmatic).</summary>
    public event EventHandler<RibbonValueChangedEventArgs>? ValueChanged;

    /// <summary>
    /// Raised when the user commits a value (arrows, keys, wheel, typed text, end of a label scrub); this is when the
    /// command executes. Programmatic / bound changes only raise <see cref="ValueChanged"/>.
    /// </summary>
    public event EventHandler<RibbonValueChangedEventArgs>? ValueCommitted;

    /// <summary>Value (kept within <see cref="Minimum"/> and <see cref="Maximum"/>).</summary>
    public double Value { get => (double)GetValue(ValueProperty); set => SetValue(ValueProperty, Coerce(value)); }

    /// <summary>Minimum.</summary>
    public double Minimum { get => (double)GetValue(MinimumProperty); set => SetValue(MinimumProperty, value); }

    /// <summary>Maximum.</summary>
    public double Maximum { get => (double)GetValue(MaximumProperty); set => SetValue(MaximumProperty, value); }

    /// <summary>Step for arrows, keys and wheel (PageUp / PageDown use 10x).</summary>
    public double Increment { get => (double)GetValue(IncrementProperty); set => SetValue(IncrementProperty, value); }

    /// <summary>Numeric format.</summary>
    public string Format { get => (string)GetValue(FormatProperty); set => SetValue(FormatProperty, value); }

    /// <summary>Unit suffix ("pt", "cm", "\"", "°", "%").</summary>
    public string? Unit { get => (string?)GetValue(UnitProperty); set => SetValue(UnitProperty, value); }

    /// <summary>Allows dragging the label horizontally to change the value.</summary>
    public bool IsScrubEnabled { get => (bool)GetValue(IsScrubEnabledProperty); set => SetValue(IsScrubEnabledProperty, value); }

    /// <summary>Formatted value.</summary>
    public string FormatValue(double value)
        => value.ToString(Format, CultureInfo.CurrentCulture) + (string.IsNullOrEmpty(Unit) ? string.Empty : (Unit.Length > 1 ? " " : string.Empty) + Unit);

    /// <inheritdoc />
    protected override void OnApplyTemplate()
    {
        if (_textBox is not null)
        {
            _textBox.KeyDown -= OnKeyDown;
            _textBox.LostFocus -= OnLostFocus;
            _textBox.PointerWheelChanged -= OnWheel;
        }

        if (_up is not null)
        {
            _up.Click -= OnUp;
        }

        if (_down is not null)
        {
            _down.Click -= OnDown;
        }

        if (_scrubHandle is not null)
        {
            _scrubHandle.PointerPressed -= OnScrubPressed;
            _scrubHandle.PointerMoved -= OnScrubMoved;
            _scrubHandle.PointerReleased -= OnScrubReleased;
        }

        base.OnApplyTemplate();
        _textBox = GetTemplateChild("PART_TextBox") as TextBox;
        _up = GetTemplateChild("PART_UpButton") as RepeatButton;
        _down = GetTemplateChild("PART_DownButton") as RepeatButton;
        _scrubHandle = GetTemplateChild("PART_Label") as FrameworkElement;
        if (_textBox is not null)
        {
            _textBox.KeyDown += OnKeyDown;
            _textBox.LostFocus += OnLostFocus;
            _textBox.PointerWheelChanged += OnWheel;
            Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(_textBox, Label ?? string.Empty);
        }

        if (_up is not null)
        {
            _up.Click += OnUp;
        }

        if (_down is not null)
        {
            _down.Click += OnDown;
        }

        if (_scrubHandle is not null)
        {
            _scrubHandle.PointerPressed += OnScrubPressed;
            _scrubHandle.PointerMoved += OnScrubMoved;
            _scrubHandle.PointerReleased += OnScrubReleased;
        }

        UpdateText();
    }

    private double Coerce(double value)
    {
        // Not Math.Clamp: it throws while Minimum and Maximum are being changed and briefly cross.
        var min = Minimum;
        var max = Math.Max(min, Maximum);
        return double.IsNaN(value) ? min : Math.Min(max, Math.Max(min, value));
    }

    private void CoerceValue()
    {
        var coerced = Coerce(Value);
        if (!coerced.Equals(Value))
        {
            Value = coerced;
        }
    }

    private void OnValueChanged(double oldValue, double newValue)
    {
        // Bindings and styles bypass the CLR setter: coerce here (the nested change raises the events).
        var coerced = Coerce(newValue);
        if (!coerced.Equals(newValue))
        {
            SetValue(ValueProperty, coerced);
            return;
        }

        UpdateText();
        ValueChanged?.Invoke(this, new RibbonValueChangedEventArgs(oldValue, newValue));
    }

    private void UpdateText()
    {
        if (_textBox is null)
        {
            return;
        }

        // Keep what the user is typing; the focused element can only be queried while the input is in a XamlRoot.
        var root = XamlRoot ?? _textBox.XamlRoot;
        if (root is null || !ReferenceEquals(FocusManager.GetFocusedElement(root), _textBox))
        {
            _textBox.Text = FormatValue(Value);
        }
    }

    private void Step(double delta)
    {
        var old = Value;
        Value += delta;
        if (_textBox is not null)
        {
            _textBox.Text = FormatValue(Value);
        }

        Commit(old);
    }

    // A user-originated change: executes the command and raises ValueCommitted.
    private void Commit(double oldValue)
    {
        ExecuteCommand(Value);
        ValueCommitted?.Invoke(this, new RibbonValueChangedEventArgs(oldValue, Value));
    }

    private void OnUp(object sender, RoutedEventArgs e) => Step(Increment);

    private void OnDown(object sender, RoutedEventArgs e) => Step(-Increment);

    private void OnWheel(object sender, PointerRoutedEventArgs e)
    {
        var delta = e.GetCurrentPoint(this).Properties.MouseWheelDelta;
        Step(delta > 0 ? Increment : -Increment);
        e.Handled = true;
    }

    private void OnKeyDown(object sender, KeyRoutedEventArgs e)
    {
        switch (e.Key)
        {
            case VirtualKey.Up:
                Step(Increment);
                e.Handled = true;
                break;
            case VirtualKey.Down:
                Step(-Increment);
                e.Handled = true;
                break;
            case VirtualKey.PageUp:
                Step(Increment * 10);
                e.Handled = true;
                break;
            case VirtualKey.PageDown:
                Step(-Increment * 10);
                e.Handled = true;
                break;
            case VirtualKey.Enter:
                CommitText();
                e.Handled = true;
                break;
            case VirtualKey.Escape:
                if (_textBox is not null)
                {
                    _textBox.Text = FormatValue(Value);
                }

                break;
        }
    }

    private void OnLostFocus(object sender, RoutedEventArgs e) => CommitText();

    /// <summary>Parses the text (unit suffix optional) and commits the value.</summary>
    public void CommitText()
    {
        if (_textBox is null)
        {
            return;
        }

        var text = _textBox.Text.Trim();
        if (!string.IsNullOrEmpty(Unit) && text.EndsWith(Unit, StringComparison.OrdinalIgnoreCase))
        {
            text = text[..^Unit.Length].Trim();
        }

        if (TryParseNumber(text, out var value))
        {
            var old = Value;
            var changed = Math.Abs(Coerce(value) - old) > double.Epsilon;
            Value = value;
            if (changed)
            {
                Commit(old);
            }
        }

        _textBox.Text = FormatValue(Value);
    }

    private void OnScrubPressed(object sender, PointerRoutedEventArgs e)
    {
        if (!IsScrubEnabled || _scrubHandle is null)
        {
            return;
        }

        _scrubStart = e.GetCurrentPoint(null).Position;
        _scrubValue = Value;
        _scrubOldValue = Value;
        _scrubHandle.CapturePointer(e.Pointer);
        e.Handled = true;
    }

    private void OnScrubMoved(object sender, PointerRoutedEventArgs e)
    {
        if (_scrubStart is not { } start)
        {
            return;
        }

        var dx = e.GetCurrentPoint(null).Position.X - start.X;
        Value = _scrubValue + (Math.Round(dx / 4) * Increment);
    }

    private void OnScrubReleased(object sender, PointerRoutedEventArgs e)
    {
        if (_scrubStart is null)
        {
            return;
        }

        _scrubStart = null;
        _scrubHandle?.ReleasePointerCapture(e.Pointer);
        Commit(_scrubOldValue);
    }

    /// <inheritdoc />
    protected override FrameworkElement? CreateLinkedCopyCore()
    {
        var copy = new RibbonSpinner();
        RibbonItemHelper.LinkCommon(this, copy);
        foreach (var dp in new[] { MinimumProperty, MaximumProperty, IncrementProperty, FormatProperty, UnitProperty, InputWidthProperty })
        {
            RibbonItemHelper.Link(this, copy, dp);
        }

        RibbonItemHelper.Link(this, copy, ValueProperty, twoWay: true);

        // Only user commits on the copy run the source's command (the linked value change itself must not).
        copy.ValueCommitted += (_, e) =>
        {
            ExecuteCommand(Value);
            ValueCommitted?.Invoke(this, e);
        };
        return copy;
    }

    /// <inheritdoc />
    protected override RibbonKeyTipResult OnKeyTipCore()
    {
        _textBox?.Focus(FocusState.Keyboard);
        _textBox?.SelectAll();
        return RibbonKeyTipResult.Close;
    }

    /// <inheritdoc />
    protected override bool InvokeCore()
    {
        OnKeyTipCore();
        return true;
    }
}

/// <summary>Value change arguments.</summary>
public sealed class RibbonValueChangedEventArgs(double oldValue, double newValue) : EventArgs
{
    /// <summary>Old value.</summary>
    public double OldValue { get; } = oldValue;

    /// <summary>New value.</summary>
    public double NewValue { get; } = newValue;
}

/// <summary>Single-line text input (Enter executes the command with the text).</summary>
public partial class RibbonTextBox : RibbonInputBase
{
    /// <summary>Identifies <see cref="Text"/>.</summary>
    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(nameof(Text), typeof(string), typeof(RibbonTextBox), new PropertyMetadata(string.Empty, (d, _) => ((RibbonTextBox)d).OnTextChanged()));

    private TextBox? _textBox;

    /// <summary>Creates a text box.</summary>
    public RibbonTextBox()
    {
        DefaultStyleKey = typeof(RibbonTextBox);
        InputWidth = 140;
    }

    /// <summary>Raised when Enter is pressed.</summary>
    public event EventHandler<string>? Submitted;

    /// <summary>Text.</summary>
    public string Text { get => (string)GetValue(TextProperty); set => SetValue(TextProperty, value); }

    /// <inheritdoc />
    protected override void OnApplyTemplate()
    {
        if (_textBox is not null)
        {
            _textBox.KeyDown -= OnKeyDown;
            _textBox.TextChanged -= OnInnerTextChanged;
        }

        base.OnApplyTemplate();
        _textBox = GetTemplateChild("PART_TextBox") as TextBox;
        if (_textBox is not null)
        {
            _textBox.Text = Text;
            _textBox.KeyDown += OnKeyDown;
            _textBox.TextChanged += OnInnerTextChanged;
            Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(_textBox, Label ?? PlaceholderText ?? string.Empty);
        }
    }

    private void OnTextChanged()
    {
        if (_textBox is not null && _textBox.Text != Text)
        {
            _textBox.Text = Text;
        }
    }

    private void OnInnerTextChanged(object sender, TextChangedEventArgs e) => Text = _textBox?.Text ?? string.Empty;

    private void OnKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Enter)
        {
            Submitted?.Invoke(this, Text);
            ExecuteCommand(Text);
            e.Handled = true;
        }
    }

    /// <inheritdoc />
    protected override FrameworkElement? CreateLinkedCopyCore()
    {
        var copy = new RibbonTextBox();
        RibbonItemHelper.LinkCommon(this, copy);
        RibbonItemHelper.Link(this, copy, InputWidthProperty);
        RibbonItemHelper.Link(this, copy, PlaceholderTextProperty);
        RibbonItemHelper.Link(this, copy, TextProperty, twoWay: true);
        copy.Submitted += (_, text) =>
        {
            Submitted?.Invoke(this, text);
            ExecuteCommand(text);
        };
        return copy;
    }

    /// <inheritdoc />
    protected override RibbonKeyTipResult OnKeyTipCore()
    {
        _textBox?.Focus(FocusState.Keyboard);
        return RibbonKeyTipResult.Close;
    }

    /// <inheritdoc />
    protected override bool InvokeCore()
    {
        _textBox?.Focus(FocusState.Keyboard);
        return true;
    }
}

/// <summary>Slider item (zoom, opacity, brush size).</summary>
public partial class RibbonSlider : RibbonInputBase
{
    /// <summary>Identifies <see cref="Value"/>.</summary>
    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(nameof(Value), typeof(double), typeof(RibbonSlider), new PropertyMetadata(0d, (d, e) => ((RibbonSlider)d).OnValueChanged((double)e.NewValue)));

    /// <summary>Identifies <see cref="Minimum"/>.</summary>
    public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register(nameof(Minimum), typeof(double), typeof(RibbonSlider), new PropertyMetadata(0d, (d, _) => ((RibbonSlider)d).SyncSlider()));

    /// <summary>Identifies <see cref="Maximum"/>.</summary>
    public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(nameof(Maximum), typeof(double), typeof(RibbonSlider), new PropertyMetadata(100d, (d, _) => ((RibbonSlider)d).SyncSlider()));

    /// <summary>Identifies <see cref="StepFrequency"/>.</summary>
    public static readonly DependencyProperty StepFrequencyProperty = DependencyProperty.Register(nameof(StepFrequency), typeof(double), typeof(RibbonSlider), new PropertyMetadata(1d, (d, _) => ((RibbonSlider)d).SyncSlider()));

    private Slider? _slider;

    // True while values are pushed into the template slider (programmatic: no command), or pulled back from it.
    private bool _pushing;
    private bool _pulling;

    /// <summary>Creates a slider.</summary>
    public RibbonSlider()
    {
        DefaultStyleKey = typeof(RibbonSlider);
        InputWidth = 120;
    }

    /// <summary>Raised when the value changes (user or programmatic).</summary>
    public event EventHandler<double>? ValueChanged;

    /// <summary>
    /// Raised when the user changes the value with the slider (drag, track click, keys); this is when the command
    /// executes. Programmatic / bound changes only raise <see cref="ValueChanged"/>.
    /// </summary>
    public event EventHandler<double>? ValueCommitted;

    /// <summary>Value.</summary>
    public double Value { get => (double)GetValue(ValueProperty); set => SetValue(ValueProperty, value); }

    /// <summary>Minimum.</summary>
    public double Minimum { get => (double)GetValue(MinimumProperty); set => SetValue(MinimumProperty, value); }

    /// <summary>Maximum.</summary>
    public double Maximum { get => (double)GetValue(MaximumProperty); set => SetValue(MaximumProperty, value); }

    /// <summary>Step.</summary>
    public double StepFrequency { get => (double)GetValue(StepFrequencyProperty); set => SetValue(StepFrequencyProperty, value); }

    /// <inheritdoc />
    protected override void OnApplyTemplate()
    {
        if (_slider is not null)
        {
            _slider.ValueChanged -= OnSliderChanged;
        }

        base.OnApplyTemplate();
        _slider = GetTemplateChild("PART_Slider") as Slider;
        if (_slider is not null)
        {
            SyncSlider();
            _slider.ValueChanged += OnSliderChanged;
            Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(_slider, Label ?? string.Empty);
        }
    }

    private void OnValueChanged(double value)
    {
        if (!_pulling)
        {
            SyncSlider();
        }

        ValueChanged?.Invoke(this, value);
    }

    // The template slider is synchronized in code (not with a TwoWay binding) so programmatic changes and template
    // loading can be told apart from user interaction.
    private void SyncSlider()
    {
        if (_slider is null || _pushing)
        {
            return;
        }

        var minimum = Minimum;
        var maximum = Math.Max(minimum, Maximum);
        var value = double.IsNaN(Value) ? minimum : Math.Min(maximum, Math.Max(minimum, Value));
        _pushing = true;
        try
        {
            _slider.Minimum = minimum;
            _slider.Maximum = maximum;
            _slider.StepFrequency = StepFrequency;
            _slider.Value = value;
        }
        finally
        {
            _pushing = false;
        }

        if (!value.Equals(Value))
        {
            // Out of range (e.g. after Minimum / Maximum changed): coerced without running the command.
            Pull(value);
        }
    }

    private void Pull(double value)
    {
        _pulling = true;
        try
        {
            Value = value;
        }
        finally
        {
            _pulling = false;
        }
    }

    private void OnSliderChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        if (_pushing)
        {
            return;
        }

        Pull(e.NewValue);
        ExecuteCommand(e.NewValue);
        ValueCommitted?.Invoke(this, e.NewValue);
    }

    /// <inheritdoc />
    protected override FrameworkElement? CreateLinkedCopyCore()
    {
        var copy = new RibbonSlider();
        RibbonItemHelper.LinkCommon(this, copy);
        foreach (var dp in new[] { MinimumProperty, MaximumProperty, StepFrequencyProperty, InputWidthProperty })
        {
            RibbonItemHelper.Link(this, copy, dp);
        }

        RibbonItemHelper.Link(this, copy, ValueProperty, twoWay: true);

        // User changes on the copy run the source's command once.
        copy.ValueCommitted += (_, value) =>
        {
            ExecuteCommand(value);
            ValueCommitted?.Invoke(this, value);
        };
        return copy;
    }
}
