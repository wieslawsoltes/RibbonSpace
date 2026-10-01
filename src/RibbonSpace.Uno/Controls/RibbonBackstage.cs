using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using RibbonSpace.Localization;

namespace RibbonSpace.Controls;

/// <summary>Navigation entry of a <see cref="RibbonBackstage"/>: a page (Content) or an action (Command).</summary>
[ContentProperty(Name = nameof(Content))]
public partial class RibbonBackstageItem : DependencyObject
{
    /// <summary>Identifies <see cref="Header"/>.</summary>
    public static readonly DependencyProperty HeaderProperty = DependencyProperty.Register(nameof(Header), typeof(string), typeof(RibbonBackstageItem), new PropertyMetadata(null, OnChanged));

    /// <summary>Identifies <see cref="Icon"/>.</summary>
    public static readonly DependencyProperty IconProperty = DependencyProperty.Register(nameof(Icon), typeof(object), typeof(RibbonBackstageItem), new PropertyMetadata(null, OnChanged));

    /// <summary>Identifies <see cref="Content"/>.</summary>
    public static readonly DependencyProperty ContentProperty = DependencyProperty.Register(nameof(Content), typeof(object), typeof(RibbonBackstageItem), new PropertyMetadata(null, OnChanged));

    /// <summary>Identifies <see cref="ContentTemplate"/>.</summary>
    public static readonly DependencyProperty ContentTemplateProperty = DependencyProperty.Register(nameof(ContentTemplate), typeof(DataTemplate), typeof(RibbonBackstageItem), new PropertyMetadata(null, OnChanged));

    /// <summary>Identifies <see cref="Command"/>.</summary>
    public static readonly DependencyProperty CommandProperty = DependencyProperty.Register(nameof(Command), typeof(ICommand), typeof(RibbonBackstageItem), new PropertyMetadata(null, OnChanged));

    /// <summary>Identifies <see cref="CommandParameter"/>.</summary>
    public static readonly DependencyProperty CommandParameterProperty = DependencyProperty.Register(nameof(CommandParameter), typeof(object), typeof(RibbonBackstageItem), new PropertyMetadata(null));

    /// <summary>Identifies <see cref="Placement"/>.</summary>
    public static readonly DependencyProperty PlacementProperty = DependencyProperty.Register(nameof(Placement), typeof(RibbonBackstagePlacement), typeof(RibbonBackstageItem), new PropertyMetadata(RibbonBackstagePlacement.Top, OnChanged));

    /// <summary>Identifies <see cref="KeyTip"/>.</summary>
    public static readonly DependencyProperty KeyTipProperty = DependencyProperty.Register(nameof(KeyTip), typeof(string), typeof(RibbonBackstageItem), new PropertyMetadata(null, OnChanged));

    /// <summary>Identifies <see cref="HasSeparatorBefore"/>.</summary>
    public static readonly DependencyProperty HasSeparatorBeforeProperty = DependencyProperty.Register(nameof(HasSeparatorBefore), typeof(bool), typeof(RibbonBackstageItem), new PropertyMetadata(false, OnChanged));

    /// <summary>Identifies <see cref="ClosesBackstage"/>.</summary>
    public static readonly DependencyProperty ClosesBackstageProperty = DependencyProperty.Register(nameof(ClosesBackstage), typeof(bool), typeof(RibbonBackstageItem), new PropertyMetadata(true));

    /// <summary>Identifies <see cref="IsEnabled"/>.</summary>
    public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.Register(nameof(IsEnabled), typeof(bool), typeof(RibbonBackstageItem), new PropertyMetadata(true, OnChanged));

    /// <summary>Identifies <see cref="IsVisible"/>.</summary>
    public static readonly DependencyProperty IsVisibleProperty = DependencyProperty.Register(nameof(IsVisible), typeof(bool), typeof(RibbonBackstageItem), new PropertyMetadata(true, OnChanged));

    /// <summary>Identifies <see cref="ScreenTip"/>.</summary>
    public static readonly DependencyProperty ScreenTipProperty = DependencyProperty.Register(nameof(ScreenTip), typeof(object), typeof(RibbonBackstageItem), new PropertyMetadata(null, OnChanged));

    /// <summary>Identifies <see cref="Id"/>.</summary>
    public static readonly DependencyProperty IdProperty = DependencyProperty.Register(nameof(Id), typeof(string), typeof(RibbonBackstageItem), new PropertyMetadata(null, OnChanged));

    /// <summary>Header.</summary>
    public string? Header { get => (string?)GetValue(HeaderProperty); set => SetValue(HeaderProperty, value); }

    /// <summary>Icon.</summary>
    public object? Icon { get => GetValue(IconProperty); set => SetValue(IconProperty, value); }

    /// <summary>Page content (element or data with <see cref="ContentTemplate"/>). Items without content are actions.</summary>
    public object? Content { get => GetValue(ContentProperty); set => SetValue(ContentProperty, value); }

    /// <summary>Template for non-element content.</summary>
    public DataTemplate? ContentTemplate { get => (DataTemplate?)GetValue(ContentTemplateProperty); set => SetValue(ContentTemplateProperty, value); }

    /// <summary>Action command (Save, Close).</summary>
    public ICommand? Command { get => (ICommand?)GetValue(CommandProperty); set => SetValue(CommandProperty, value); }

    /// <summary>Command parameter.</summary>
    public object? CommandParameter { get => GetValue(CommandParameterProperty); set => SetValue(CommandParameterProperty, value); }

    /// <summary>Main list or footer list.</summary>
    public RibbonBackstagePlacement Placement { get => (RibbonBackstagePlacement)GetValue(PlacementProperty); set => SetValue(PlacementProperty, value); }

    /// <summary>KeyTip.</summary>
    public string? KeyTip { get => (string?)GetValue(KeyTipProperty); set => SetValue(KeyTipProperty, value); }

    /// <summary>Draws a separator above the item.</summary>
    public bool HasSeparatorBefore { get => (bool)GetValue(HasSeparatorBeforeProperty); set => SetValue(HasSeparatorBeforeProperty, value); }

    /// <summary>Actions close the backstage after executing.</summary>
    public bool ClosesBackstage { get => (bool)GetValue(ClosesBackstageProperty); set => SetValue(ClosesBackstageProperty, value); }

    /// <summary>Enabled state.</summary>
    public bool IsEnabled { get => (bool)GetValue(IsEnabledProperty); set => SetValue(IsEnabledProperty, value); }

    /// <summary>Stable id.</summary>
    public string? Id { get => (string?)GetValue(IdProperty); set => SetValue(IdProperty, value); }

    /// <summary>Shows the item in the navigation pane.</summary>
    public bool IsVisible { get => (bool)GetValue(IsVisibleProperty); set => SetValue(IsVisibleProperty, value); }

    /// <summary>Tooltip of the navigation button (string, <see cref="RibbonScreenTip"/> or any content).</summary>
    public object? ScreenTip { get => GetValue(ScreenTipProperty); set => SetValue(ScreenTipProperty, value); }

    /// <summary>True for page items.</summary>
    public bool IsPage => Content is not null || ContentTemplate is not null;

    /// <summary>Navigation button generated for this item.</summary>
    public Button? NavigationButton { get; internal set; }

    /// <summary>Backstage hosting the item.</summary>
    internal RibbonBackstage? Owner { get; set; }

    internal Primitives.RibbonIconPresenter? NavigationIcon { get; set; }

    internal TextBlock? NavigationText { get; set; }

    private static void OnChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var item = (RibbonBackstageItem)d;
        item.Owner?.OnItemPropertyChanged(item, e.Property);
    }
}

/// <summary>
/// Backstage (File) view: full-window navigation pane with pages (Home, New, Open, Info...), actions (Save, Close)
/// and footer items (Account, Options). Hosted by <see cref="Ribbon.Backstage"/> or used standalone.
/// </summary>
[ContentProperty(Name = nameof(Items))]
public partial class RibbonBackstage : Control
{
    /// <summary>Identifies <see cref="SelectedItem"/>.</summary>
    public static readonly DependencyProperty SelectedItemProperty = DependencyProperty.Register(nameof(SelectedItem), typeof(RibbonBackstageItem), typeof(RibbonBackstage), new PropertyMetadata(null, (d, _) => ((RibbonBackstage)d).OnSelectedItemChanged()));

    /// <summary>Identifies <see cref="Title"/>.</summary>
    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(nameof(Title), typeof(string), typeof(RibbonBackstage), new PropertyMetadata(null));

    /// <summary>Identifies <see cref="NavigationPaneWidth"/>.</summary>
    public static readonly DependencyProperty NavigationPaneWidthProperty = DependencyProperty.Register(nameof(NavigationPaneWidth), typeof(double), typeof(RibbonBackstage), new PropertyMetadata(220d));

    /// <summary>Identifies <see cref="ShowPageTitle"/>.</summary>
    public static readonly DependencyProperty ShowPageTitleProperty = DependencyProperty.Register(nameof(ShowPageTitle), typeof(bool), typeof(RibbonBackstage), new PropertyMetadata(true, (d, _) => ((RibbonBackstage)d).OnSelectedItemChanged()));

    /// <summary>Identifies <see cref="PaneHeader"/>.</summary>
    public static readonly DependencyProperty PaneHeaderProperty = DependencyProperty.Register(nameof(PaneHeader), typeof(object), typeof(RibbonBackstage), new PropertyMetadata(null));

    /// <summary>Identifies <see cref="PageTitle"/>.</summary>
    public static readonly DependencyProperty PageTitleProperty = DependencyProperty.Register(nameof(PageTitle), typeof(string), typeof(RibbonBackstage), new PropertyMetadata(null));

    private readonly List<RibbonBackstageItem> _knownItems = [];
    private StackPanel? _topItems;
    private StackPanel? _bottomItems;
    private ContentPresenter? _content;
    private Button? _backButton;
    private FrameworkElement? _pageTitle;
    private FrameworkElement? _contentRoot;
    private Ribbon? _ribbon;

    /// <summary>Creates a backstage.</summary>
    public RibbonBackstage()
    {
        DefaultStyleKey = typeof(RibbonBackstage);
        RibbonTheme.EnsureResources();
        Items.CollectionChanged += OnItemsChanged;
        KeyDown += OnKeyDown;
        ActualThemeChanged += (_, _) => UpdateSelectionVisuals();
        IsTabStop = true;
        TabFocusNavigation = KeyboardNavigationMode.Cycle;
    }

    /// <summary>Raised when the back button or Esc asks to close.</summary>
    public event EventHandler? CloseRequested;

    /// <summary>Raised when an item is invoked (page selected or action executed).</summary>
    public event EventHandler<RibbonBackstageItem>? ItemInvoked;

    /// <summary>Items.</summary>
    public ObservableCollection<RibbonBackstageItem> Items { get; } = [];

    /// <summary>Selected page.</summary>
    public RibbonBackstageItem? SelectedItem { get => (RibbonBackstageItem?)GetValue(SelectedItemProperty); set => SetValue(SelectedItemProperty, value); }

    /// <summary>Title shown at the top of the navigation pane (application name).</summary>
    public string? Title { get => (string?)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }

    /// <summary>Width of the navigation pane.</summary>
    public double NavigationPaneWidth { get => (double)GetValue(NavigationPaneWidthProperty); set => SetValue(NavigationPaneWidthProperty, value); }

    /// <summary>Shows the selected page header as a large title.</summary>
    public bool ShowPageTitle { get => (bool)GetValue(ShowPageTitleProperty); set => SetValue(ShowPageTitleProperty, value); }

    /// <summary>Custom content above the navigation items (logo, account).</summary>
    public object? PaneHeader { get => GetValue(PaneHeaderProperty); set => SetValue(PaneHeaderProperty, value); }

    /// <summary>Title of the selected page (template use).</summary>
    public string? PageTitle { get => (string?)GetValue(PageTitleProperty); private set => SetValue(PageTitleProperty, value); }

    /// <summary>Owning ribbon (the backstage follows its light / dark theme while open).</summary>
    public Ribbon? Ribbon
    {
        get => _ribbon;
        internal set
        {
            if (ReferenceEquals(_ribbon, value))
            {
                return;
            }

            if (_ribbon is not null)
            {
                _ribbon.ActualThemeChanged -= OnRibbonThemeChanged;
            }

            _ribbon = value;
            if (_ribbon is not null)
            {
                _ribbon.ActualThemeChanged += OnRibbonThemeChanged;
            }
        }
    }

    /// <summary>The back button.</summary>
    public Button? BackButton => _backButton;

    private void OnRibbonThemeChanged(FrameworkElement sender, object args)
    {
        if (IsLoaded && _ribbon is not null && RequestedTheme != _ribbon.ActualTheme)
        {
            RequestedTheme = _ribbon.ActualTheme;
        }
    }

    /// <inheritdoc />
    protected override void OnApplyTemplate()
    {
        if (_backButton is not null)
        {
            _backButton.Click -= OnBackClick;
        }

        _topItems?.Children.Clear();
        _bottomItems?.Children.Clear();
        base.OnApplyTemplate();
        _topItems = GetTemplateChild("PART_TopItems") as StackPanel;
        _bottomItems = GetTemplateChild("PART_BottomItems") as StackPanel;
        _content = GetTemplateChild("PART_Content") as ContentPresenter;
        _backButton = GetTemplateChild("PART_BackButton") as Button;
        _pageTitle = GetTemplateChild("PART_PageTitle") as FrameworkElement;
        _contentRoot = GetTemplateChild("PART_ContentRoot") as FrameworkElement;
        if (_backButton is not null)
        {
            _backButton.Click += OnBackClick;
            AutomationProperties.SetName(_backButton, RibbonStrings.Current.Back);
            ToolTipService.SetToolTip(_backButton, RibbonStrings.Current.Back);
        }

        BuildNavigation();
        EnsureSelection();
        OnSelectedItemChanged();
    }

    /// <inheritdoc />
    protected override void OnGotFocus(RoutedEventArgs e)
    {
        base.OnGotFocus(e);

        // Opening focuses the backstage itself: move keyboard focus to the selected (or first) navigation item.
        if (ReferenceEquals(e.OriginalSource, this))
        {
            FocusSelectedItem();
        }
    }

    /// <summary>Moves keyboard focus to the selected navigation item (or the first available one).</summary>
    public bool FocusSelectedItem()
    {
        var target = SelectedItem?.NavigationButton is { } selected && IsNavigable(selected) ? selected : NavigationButtons().FirstOrDefault(IsNavigable);
        return target?.Focus(FocusState.Keyboard) == true;
    }

    private void OnBackClick(object sender, RoutedEventArgs e) => CloseRequested?.Invoke(this, EventArgs.Empty);

    private void OnKeyDown(object sender, KeyRoutedEventArgs e)
    {
        switch (e.Key)
        {
            case Windows.System.VirtualKey.Escape:
                CloseRequested?.Invoke(this, EventArgs.Empty);
                e.Handled = true;
                break;
            case Windows.System.VirtualKey.Up or Windows.System.VirtualKey.Down:
                if (MoveNavigationFocus(e.Key == Windows.System.VirtualKey.Down ? 1 : -1))
                {
                    e.Handled = true;
                }

                break;
        }
    }

    private IEnumerable<Button> NavigationButtons()
        => Items.Where(i => i.Placement != RibbonBackstagePlacement.Bottom).Concat(Items.Where(i => i.Placement == RibbonBackstagePlacement.Bottom))
            .Select(i => i.NavigationButton).OfType<Button>();

    private static bool IsNavigable(Button button) => button.Visibility == Visibility.Visible && button.IsEnabled;

    private bool MoveNavigationFocus(int delta)
    {
        if (XamlRoot is null)
        {
            return false;
        }

        // Only when focus is in the navigation pane (pages handle their own arrow keys).
        var buttons = NavigationButtons().Where(IsNavigable).ToList();
        var focused = FocusManager.GetFocusedElement(XamlRoot) as Button;
        var index = focused is null ? -1 : buttons.IndexOf(focused);
        if (index < 0 || buttons.Count == 0)
        {
            return false;
        }

        var next = (index + delta + buttons.Count) % buttons.Count;
        return buttons[next].Focus(FocusState.Keyboard);
    }

    private void OnItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        foreach (var removed in _knownItems.Where(i => !Items.Contains(i)).ToArray())
        {
            if (ReferenceEquals(removed.Owner, this))
            {
                removed.Owner = null;
            }

            removed.NavigationButton = null;
            removed.NavigationIcon = null;
            removed.NavigationText = null;
        }

        _knownItems.Clear();
        _knownItems.AddRange(Items);
        foreach (var item in Items)
        {
            item.Owner = this;
        }

        BuildNavigation();
        EnsureSelection();
    }

    /// <summary>Selects the first page when nothing (or a removed / hidden item) is selected.</summary>
    public void EnsureSelection()
    {
        if (SelectedItem is null || !Items.Contains(SelectedItem) || !SelectedItem.IsVisible || !SelectedItem.IsPage)
        {
            SelectedItem = Items.FirstOrDefault(i => i.IsPage && i.IsVisible);
        }
    }

    internal void OnItemPropertyChanged(RibbonBackstageItem item, DependencyProperty property)
    {
        if (property == RibbonBackstageItem.HeaderProperty)
        {
            if (item.NavigationText is { } text)
            {
                text.Text = item.Header ?? string.Empty;
            }

            if (item.NavigationButton is { } button)
            {
                AutomationProperties.SetName(button, item.Header ?? string.Empty);
            }

            if (ReferenceEquals(item, SelectedItem))
            {
                PageTitle = item.Header;
            }
        }
        else if (property == RibbonBackstageItem.IconProperty)
        {
            if (item.NavigationIcon is { } icon)
            {
                icon.Icon = item.Icon;
            }
        }
        else if (property == RibbonBackstageItem.IsEnabledProperty)
        {
            if (item.NavigationButton is { } button)
            {
                button.IsEnabled = item.IsEnabled;
            }
        }
        else if (property == RibbonBackstageItem.IdProperty)
        {
            if (item.NavigationButton is { } button)
            {
                ApplyAutomationId(button, item);
            }
        }
        else if (property == RibbonBackstageItem.ScreenTipProperty)
        {
            if (item.NavigationButton is { } button)
            {
                ToolTipService.SetToolTip(button, item.ScreenTip);
            }
        }
        else if (property == RibbonBackstageItem.ContentProperty || property == RibbonBackstageItem.ContentTemplateProperty)
        {
            // Pages can become actions (and back): selection and the shown content follow.
            if (ReferenceEquals(item, SelectedItem))
            {
                if (item.IsPage)
                {
                    OnSelectedItemChanged();
                }
                else
                {
                    EnsureSelection();
                }
            }
            else
            {
                EnsureSelection();
            }
        }
        else if (property == RibbonBackstageItem.IsVisibleProperty || property == RibbonBackstageItem.PlacementProperty || property == RibbonBackstageItem.HasSeparatorBeforeProperty)
        {
            BuildNavigation();
            EnsureSelection();
        }
    }

    private void BuildNavigation()
    {
        if (_topItems is null || _bottomItems is null)
        {
            return;
        }

        _topItems.Children.Clear();
        _bottomItems.Children.Clear();
        foreach (var item in Items)
        {
            var host = item.Placement == RibbonBackstagePlacement.Bottom ? _bottomItems : _topItems;
            if (item.HasSeparatorBefore && item.IsVisible)
            {
                var separator = new Microsoft.UI.Xaml.Shapes.Rectangle { Height = 1, Margin = new Thickness(16, 6, 16, 6), Opacity = 0.35 };
                RibbonTheme.SetThemeBrush(separator, Microsoft.UI.Xaml.Shapes.Shape.FillProperty, "RibbonBackstagePaneForegroundBrush");
                host.Children.Add(separator);
            }

            var button = new Button { Tag = item };
            if (Application.Current.Resources.TryGetValue("RibbonBackstageNavButtonStyle", out var style))
            {
                button.Style = (Style)style;
            }

            var icon = new Primitives.RibbonIconPresenter { Icon = item.Icon, IconSize = 18, Width = 18, Height = 18 };
            icon.SetBinding(Primitives.RibbonIconPresenter.ForegroundProperty, new Microsoft.UI.Xaml.Data.Binding { Source = button, Path = new PropertyPath("Foreground") });
            var text = new TextBlock { Text = item.Header, VerticalAlignment = VerticalAlignment.Center };
            button.Content = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 12,
                Children =
                {
                    new Border { Width = 18, Height = 18, Child = icon },
                    text,
                },
            };
            button.IsEnabled = item.IsEnabled;
            button.Visibility = item.IsVisible ? Visibility.Visible : Visibility.Collapsed;
            AutomationProperties.SetName(button, item.Header ?? string.Empty);
            ApplyAutomationId(button, item);
            if (item.ScreenTip is not null)
            {
                ToolTipService.SetToolTip(button, item.ScreenTip);
            }

            button.Click += (_, _) => Invoke(item);
            item.NavigationButton = button;
            item.NavigationIcon = icon;
            item.NavigationText = text;
            host.Children.Add(button);
        }

        UpdateSelectionVisuals();
    }

    private static void ApplyAutomationId(Button button, RibbonBackstageItem item)
    {
        if (item.Id is not null)
        {
            AutomationProperties.SetAutomationId(button, "Backstage_" + item.Id);
        }
        else
        {
            button.ClearValue(AutomationProperties.AutomationIdProperty);
        }
    }

    /// <summary>Selects a page or executes an action item.</summary>
    public void Invoke(RibbonBackstageItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (!item.IsEnabled || !item.IsVisible)
        {
            return;
        }

        ItemInvoked?.Invoke(this, item);
        if (item.IsPage)
        {
            SelectedItem = item;
            return;
        }

        if (item.Command is { } command && command.CanExecute(item.CommandParameter))
        {
            command.Execute(item.CommandParameter);
        }

        Ribbon?.OnItemInvoked(item.NavigationButton ?? (FrameworkElement)this, item.Id, item.CommandParameter);
        if (item.ClosesBackstage)
        {
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    private void OnSelectedItemChanged()
    {
        var item = SelectedItem;
        PageTitle = item?.Header;
        if (_pageTitle is not null)
        {
            _pageTitle.Visibility = ShowPageTitle && item is not null ? Visibility.Visible : Visibility.Collapsed;
        }

        if (_content is not null)
        {
            _content.ContentTemplate = item?.ContentTemplate;
            _content.Content = item?.Content;
        }

        UpdateSelectionVisuals();
        AnimateContent();
    }

    private void UpdateSelectionVisuals()
    {
        var selectedBrush = RibbonTheme.GetBrush(this, "RibbonBackstagePaneSelectedBrush");
        foreach (var item in Items)
        {
            if (item.NavigationButton is { } button)
            {
                var selected = ReferenceEquals(item, SelectedItem);
                VisualStateManager.GoToState(button, selected ? "Selected" : "Unselected", false);
                button.Tag = item;
                button.Background = selected && selectedBrush is not null ? selectedBrush : new SolidColorBrush(Microsoft.UI.Colors.Transparent);
            }
        }
    }

    /// <summary>Plays the entrance animation of the content area.</summary>
    public void AnimateContent()
    {
        if (_contentRoot is null)
        {
            return;
        }

        var transform = new TranslateTransform { X = 24 };
        _contentRoot.RenderTransform = transform;
        _contentRoot.Opacity = 0;
        var storyboard = new Storyboard();
        var fade = new DoubleAnimation { From = 0, To = 1, Duration = TimeSpan.FromMilliseconds(180) };
        Storyboard.SetTarget(fade, _contentRoot);
        Storyboard.SetTargetProperty(fade, "Opacity");
        var slide = new DoubleAnimation { From = 24, To = 0, Duration = TimeSpan.FromMilliseconds(220), EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
        Storyboard.SetTarget(slide, transform);
        Storyboard.SetTargetProperty(slide, "X");
        storyboard.Children.Add(fade);
        storyboard.Children.Add(slide);
        storyboard.Completed += (_, _) => _contentRoot.Opacity = 1;
        storyboard.Begin();
    }

    /// <summary>KeyTip targets (navigation buttons and back button).</summary>
    internal IEnumerable<FrameworkElement> GetKeyTipTargets()
    {
        if (_backButton is not null)
        {
            yield return _backButton;
        }

        foreach (var item in Items)
        {
            if (item is { IsVisible: true, NavigationButton: { } button })
            {
                yield return button;
            }
        }
    }
}
