using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using RibbonSpace.Controls.Primitives;
using RibbonSpace.Localization;
using RibbonSpace.Search;
using Windows.System;

namespace RibbonSpace.Controls;

/// <summary>A command of the <see cref="RibbonApplicationMenu"/> (left column), optionally with sub-commands.</summary>
[ContentProperty(Name = nameof(Items))]
public partial class RibbonApplicationMenuItem : DependencyObject
{
    /// <summary>Identifies <see cref="Label"/>.</summary>
    public static readonly DependencyProperty LabelProperty = DependencyProperty.Register(nameof(Label), typeof(string), typeof(RibbonApplicationMenuItem), new PropertyMetadata(null, OnChanged));

    /// <summary>Identifies <see cref="Icon"/>.</summary>
    public static readonly DependencyProperty IconProperty = DependencyProperty.Register(nameof(Icon), typeof(object), typeof(RibbonApplicationMenuItem), new PropertyMetadata(null, OnChanged));

    /// <summary>Identifies <see cref="Description"/>.</summary>
    public static readonly DependencyProperty DescriptionProperty = DependencyProperty.Register(nameof(Description), typeof(string), typeof(RibbonApplicationMenuItem), new PropertyMetadata(null, OnChanged));

    /// <summary>Identifies <see cref="Command"/>.</summary>
    public static readonly DependencyProperty CommandProperty = DependencyProperty.Register(nameof(Command), typeof(ICommand), typeof(RibbonApplicationMenuItem), new PropertyMetadata(null));

    /// <summary>Identifies <see cref="CommandParameter"/>.</summary>
    public static readonly DependencyProperty CommandParameterProperty = DependencyProperty.Register(nameof(CommandParameter), typeof(object), typeof(RibbonApplicationMenuItem), new PropertyMetadata(null));

    /// <summary>Identifies <see cref="IsEnabled"/>.</summary>
    public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.Register(nameof(IsEnabled), typeof(bool), typeof(RibbonApplicationMenuItem), new PropertyMetadata(true, OnChanged));

    /// <summary>Identifies <see cref="HasSeparatorBefore"/>.</summary>
    public static readonly DependencyProperty HasSeparatorBeforeProperty = DependencyProperty.Register(nameof(HasSeparatorBefore), typeof(bool), typeof(RibbonApplicationMenuItem), new PropertyMetadata(false, OnChanged));

    /// <summary>Creates an item.</summary>
    public RibbonApplicationMenuItem() => Items.CollectionChanged += (_, _) => Changed?.Invoke(this, EventArgs.Empty);

    /// <summary>Raised when the item is invoked.</summary>
    public event EventHandler<RibbonApplicationMenuItemEventArgs>? Click;

    internal event EventHandler? Changed;

    /// <summary>Stable id (routing in <see cref="RibbonApplicationMenu.ItemInvoked"/>).</summary>
    public string? Id { get; set; }

    /// <summary>Label.</summary>
    public string? Label { get => (string?)GetValue(LabelProperty); set => SetValue(LabelProperty, value); }

    /// <summary>Icon (any RibbonSpace icon value).</summary>
    public object? Icon { get => GetValue(IconProperty); set => SetValue(IconProperty, value); }

    /// <summary>Description shown when the item is listed as a sub-command.</summary>
    public string? Description { get => (string?)GetValue(DescriptionProperty); set => SetValue(DescriptionProperty, value); }

    /// <summary>Command executed on click (items with sub-commands open them instead).</summary>
    public ICommand? Command { get => (ICommand?)GetValue(CommandProperty); set => SetValue(CommandProperty, value); }

    /// <summary>Command parameter.</summary>
    public object? CommandParameter { get => GetValue(CommandParameterProperty); set => SetValue(CommandParameterProperty, value); }

    /// <summary>Enabled state.</summary>
    public bool IsEnabled { get => (bool)GetValue(IsEnabledProperty); set => SetValue(IsEnabledProperty, value); }

    /// <summary>Draws a separator above the item.</summary>
    public bool HasSeparatorBefore { get => (bool)GetValue(HasSeparatorBeforeProperty); set => SetValue(HasSeparatorBeforeProperty, value); }

    /// <summary>Sub-commands shown in the right pane when the item is hovered (e.g. Save As → Drawing, Template...).</summary>
    public ObservableCollection<RibbonApplicationMenuItem> Items { get; } = [];

    internal void RaiseClick(object? parameter) => Click?.Invoke(this, new RibbonApplicationMenuItemEventArgs(this, parameter));

    private static void OnChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((RibbonApplicationMenuItem)d).Changed?.Invoke(d, EventArgs.Empty);
}

/// <summary>A recent document of the <see cref="RibbonApplicationMenu"/>.</summary>
public partial class RibbonApplicationMenuRecentItem : DependencyObject
{
    /// <summary>Identifies <see cref="Title"/>.</summary>
    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(nameof(Title), typeof(string), typeof(RibbonApplicationMenuRecentItem), new PropertyMetadata(null, OnChanged));

    /// <summary>Identifies <see cref="Path"/>.</summary>
    public static readonly DependencyProperty PathProperty = DependencyProperty.Register(nameof(Path), typeof(string), typeof(RibbonApplicationMenuRecentItem), new PropertyMetadata(null, OnChanged));

    /// <summary>Identifies <see cref="Icon"/>.</summary>
    public static readonly DependencyProperty IconProperty = DependencyProperty.Register(nameof(Icon), typeof(object), typeof(RibbonApplicationMenuRecentItem), new PropertyMetadata(null, OnChanged));

    /// <summary>Identifies <see cref="IsPinned"/>.</summary>
    public static readonly DependencyProperty IsPinnedProperty = DependencyProperty.Register(nameof(IsPinned), typeof(bool), typeof(RibbonApplicationMenuRecentItem), new PropertyMetadata(false, OnChanged));

    /// <summary>Identifies <see cref="Command"/>.</summary>
    public static readonly DependencyProperty CommandProperty = DependencyProperty.Register(nameof(Command), typeof(ICommand), typeof(RibbonApplicationMenuRecentItem), new PropertyMetadata(null));

    /// <summary>Raised when the document is opened from the list.</summary>
    public event EventHandler? Click;

    internal event EventHandler? Changed;

    /// <summary>Document title (file name).</summary>
    public string? Title { get => (string?)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }

    /// <summary>Location shown under the title.</summary>
    public string? Path { get => (string?)GetValue(PathProperty); set => SetValue(PathProperty, value); }

    /// <summary>Icon (defaults to a document glyph).</summary>
    public object? Icon { get => GetValue(IconProperty); set => SetValue(IconProperty, value); }

    /// <summary>Pinned documents stay at the top of the list.</summary>
    public bool IsPinned { get => (bool)GetValue(IsPinnedProperty); set => SetValue(IsPinnedProperty, value); }

    /// <summary>Command executed with the item as parameter.</summary>
    public ICommand? Command { get => (ICommand?)GetValue(CommandProperty); set => SetValue(CommandProperty, value); }

    /// <summary>Any app data (file path, document id...).</summary>
    public object? Tag { get; set; }

    internal void RaiseClick() => Click?.Invoke(this, EventArgs.Empty);

    private static void OnChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((RibbonApplicationMenuRecentItem)d).Changed?.Invoke(d, EventArgs.Empty);
}

/// <summary>Arguments of application menu item events.</summary>
public sealed class RibbonApplicationMenuItemEventArgs(RibbonApplicationMenuItem item, object? parameter) : EventArgs
{
    /// <summary>Invoked item.</summary>
    public RibbonApplicationMenuItem Item { get; } = item;

    /// <summary>Command parameter.</summary>
    public object? Parameter { get; } = parameter;
}

/// <summary>
/// Application menu in the style of AutoCAD's menu browser (and the Office 2007 application menu): a command search box,
/// commands on the left whose sub-commands open in the right pane on hover, a recent documents list with pins, and
/// footer buttons. Host it in the ribbon's <c>ApplicationMenu</c> flyout:
/// <code>
/// &lt;Ribbon.ApplicationMenu&gt;&lt;Flyout&gt;&lt;RibbonApplicationMenu Ribbon="{x:Bind Ribbon}"&gt;…&lt;/RibbonApplicationMenu&gt;&lt;/Flyout&gt;&lt;/Ribbon.ApplicationMenu&gt;
/// </code>
/// </summary>
[ContentProperty(Name = nameof(Items))]
public partial class RibbonApplicationMenu : ContentControl
{
    /// <summary>Identifies <see cref="Ribbon"/>.</summary>
    public static readonly DependencyProperty RibbonProperty = DependencyProperty.Register(nameof(Ribbon), typeof(Ribbon), typeof(RibbonApplicationMenu), new PropertyMetadata(null));

    /// <summary>Identifies <see cref="RecentHeader"/>.</summary>
    public static readonly DependencyProperty RecentHeaderProperty = DependencyProperty.Register(nameof(RecentHeader), typeof(string), typeof(RibbonApplicationMenu), new PropertyMetadata(null, (d, _) => ((RibbonApplicationMenu)d).ShowDefaultPane()));

    /// <summary>Identifies <see cref="IsSearchVisible"/>.</summary>
    public static readonly DependencyProperty IsSearchVisibleProperty = DependencyProperty.Register(nameof(IsSearchVisible), typeof(bool), typeof(RibbonApplicationMenu), new PropertyMetadata(true, (d, _) => ((RibbonApplicationMenu)d).UpdateSearchVisibility()));

    private readonly StackPanel _commands = new() { Spacing = 0, Padding = new Thickness(0, 4, 0, 4) };
    private readonly StackPanel _pane = new() { Spacing = 1 };
    private readonly TextBlock _paneHeader = new() { FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, FontSize = 13, Margin = new Thickness(10, 8, 10, 6) };
    private readonly StackPanel _footer = new() { Orientation = Orientation.Horizontal, Spacing = 6, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(8, 6, 8, 6) };
    private readonly TextBox _search = new() { MinHeight = 28, Margin = new Thickness(8, 8, 8, 4) };
    private readonly Dictionary<RibbonApplicationMenuItem, Button> _commandButtons = [];
    private RibbonApplicationMenuItem? _shownItem;

    /// <summary>Creates the menu.</summary>
    public RibbonApplicationMenu()
    {
        RibbonTheme.EnsureResources();
        IsTabStop = false;
        Items.CollectionChanged += OnItemsChanged;
        RecentItems.CollectionChanged += (_, _) => OnRecentItemsChanged();
        FooterItems.CollectionChanged += (_, _) => BuildFooter();
        _search.TextChanged += (_, _) => OnSearchTextChanged();
        _search.KeyDown += OnSearchKeyDown;
        BuildLayout();
        Loaded += (_, _) =>
        {
            _search.Text = string.Empty;
            ShowDefaultPane();
            if (IsSearchVisible)
            {
                _search.Focus(FocusState.Programmatic);
            }
        };
        AutomationProperties.SetName(this, RibbonStrings.Current.File);
    }

    /// <summary>Raised when a command (or sub-command) is invoked.</summary>
    public event EventHandler<RibbonApplicationMenuItemEventArgs>? ItemInvoked;

    /// <summary>Raised when a recent document is opened.</summary>
    public event EventHandler<RibbonApplicationMenuRecentItem>? RecentItemInvoked;

    /// <summary>Commands (left column).</summary>
    public ObservableCollection<RibbonApplicationMenuItem> Items { get; } = [];

    /// <summary>Recent documents (right pane by default). Pinned documents are listed first.</summary>
    public ObservableCollection<RibbonApplicationMenuRecentItem> RecentItems { get; } = [];

    /// <summary>Footer elements, usually buttons such as "Options" and "Exit".</summary>
    public ObservableCollection<UIElement> FooterItems { get; } = [];

    /// <summary>Ribbon whose commands the search box finds.</summary>
    public Ribbon? Ribbon { get => (Ribbon?)GetValue(RibbonProperty); set => SetValue(RibbonProperty, value); }

    /// <summary>Header of the recent documents pane (default "Recent Documents").</summary>
    public string? RecentHeader { get => (string?)GetValue(RecentHeaderProperty); set => SetValue(RecentHeaderProperty, value); }

    /// <summary>Shows the command search box.</summary>
    public bool IsSearchVisible { get => (bool)GetValue(IsSearchVisibleProperty); set => SetValue(IsSearchVisibleProperty, value); }

    /// <summary>The item whose sub-commands are shown (<c>null</c> for recent documents or search results).</summary>
    public RibbonApplicationMenuItem? ShownItem => _shownItem;

    /// <summary>Invokes a command item as if clicked (sub-commands open instead when it has any).</summary>
    public void Invoke(RibbonApplicationMenuItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (!item.IsEnabled)
        {
            return;
        }

        if (item.Items.Count > 0)
        {
            ShowSubItems(item);
            return;
        }

        CloseHost();
        item.RaiseClick(item.CommandParameter);
        if (item.Command is { } command && command.CanExecute(item.CommandParameter))
        {
            command.Execute(item.CommandParameter);
        }

        ItemInvoked?.Invoke(this, new RibbonApplicationMenuItemEventArgs(item, item.CommandParameter));
    }

    /// <summary>Opens a recent document.</summary>
    public void Invoke(RibbonApplicationMenuRecentItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        CloseHost();
        item.RaiseClick();
        if (item.Command is { } command && command.CanExecute(item))
        {
            command.Execute(item);
        }

        RecentItemInvoked?.Invoke(this, item);
    }

    private void BuildLayout()
    {
        _search.PlaceholderText = RibbonStrings.Current.SearchCommands;
        AutomationProperties.SetName(_search, RibbonStrings.Current.SearchCommands);
        var searchRow = new Border { Child = _search };
        RibbonTheme.SetThemeBrush(searchRow, Border.BackgroundProperty, "RibbonGroupCaptionBackgroundBrush");

        var left = new ScrollViewer { Content = _commands, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Width = 220 };
        _commands.XYFocusKeyboardNavigation = XYFocusKeyboardNavigationMode.Enabled;
        var divider = new Border { Width = 1 };
        RibbonTheme.SetThemeBrush(divider, Border.BackgroundProperty, "RibbonSeparatorBrush");
        _pane.XYFocusKeyboardNavigation = XYFocusKeyboardNavigationMode.Enabled;
        var right = new Grid { RowDefinitions = { new RowDefinition { Height = GridLength.Auto }, new RowDefinition { Height = new GridLength(1, GridUnitType.Star) } } };
        right.Children.Add(_paneHeader);
        var paneScroll = new ScrollViewer { Content = _pane, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Padding = new Thickness(4, 0, 4, 4) };
        Grid.SetRow(paneScroll, 1);
        right.Children.Add(paneScroll);
        var body = new Grid
        {
            MinHeight = 340,
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
            },
        };
        body.Children.Add(left);
        Grid.SetColumn(divider, 1);
        body.Children.Add(divider);
        Grid.SetColumn(right, 2);
        body.Children.Add(right);
        RibbonTheme.SetThemeBrush(_paneHeader, TextBlock.ForegroundProperty, "RibbonForegroundBrush");

        var footerRow = new Border { Child = _footer, BorderThickness = new Thickness(0, 1, 0, 0) };
        RibbonTheme.SetThemeBrush(footerRow, Border.BorderBrushProperty, "RibbonSeparatorBrush");
        RibbonTheme.SetThemeBrush(footerRow, Border.BackgroundProperty, "RibbonGroupCaptionBackgroundBrush");

        var root = new Grid
        {
            Width = 620,
            RowDefinitions =
            {
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = new GridLength(1, GridUnitType.Star) },
                new RowDefinition { Height = GridLength.Auto },
            },
        };
        root.Children.Add(searchRow);
        Grid.SetRow(body, 1);
        root.Children.Add(body);
        Grid.SetRow(footerRow, 2);
        root.Children.Add(footerRow);
        RibbonTheme.SetThemeBrush(root, Panel.BackgroundProperty, "RibbonPopupBackgroundBrush");
        Content = root;
        HorizontalContentAlignment = HorizontalAlignment.Stretch;
        VerticalContentAlignment = VerticalAlignment.Stretch;
    }

    private void UpdateSearchVisibility()
    {
        if (_search.Parent is FrameworkElement row)
        {
            row.Visibility = IsSearchVisible ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private void OnItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        foreach (var item in e.OldItems?.OfType<RibbonApplicationMenuItem>() ?? [])
        {
            item.Changed -= OnItemChanged;
        }

        foreach (var item in e.NewItems?.OfType<RibbonApplicationMenuItem>() ?? [])
        {
            item.Changed += OnItemChanged;
        }

        BuildCommands();
    }

    private void OnItemChanged(object? sender, EventArgs e)
    {
        BuildCommands();
        if (ReferenceEquals(sender, _shownItem))
        {
            ShowSubItems(_shownItem!);
        }
    }

    private void BuildCommands()
    {
        _commands.Children.Clear();
        _commandButtons.Clear();
        foreach (var item in Items)
        {
            if (item.HasSeparatorBefore && _commands.Children.Count > 0)
            {
                var line = new Border { Height = 1, Margin = new Thickness(10, 4, 10, 4) };
                RibbonTheme.SetThemeBrush(line, Border.BackgroundProperty, "RibbonSeparatorBrush");
                _commands.Children.Add(line);
            }

            var button = CreateRow(item.Icon, item.Label, null, 26, item.Items.Count > 0);
            button.Height = 44;
            button.IsEnabled = item.IsEnabled;
            AutomationProperties.SetName(button, item.Label ?? string.Empty);
            if (item.Id is { } id)
            {
                AutomationProperties.SetAutomationId(button, "AppMenu_" + id);
            }

            var captured = item;
            button.Click += (_, _) => Invoke(captured);
            button.PointerEntered += (_, _) => OnCommandHovered(captured);
            button.GotFocus += (_, _) => OnCommandHovered(captured);
            button.KeyDown += (_, e) =>
            {
                if (e.Key == VirtualKey.Right && captured.Items.Count > 0)
                {
                    ShowSubItems(captured);
                    (_pane.Children.OfType<Control>().FirstOrDefault())?.Focus(FocusState.Keyboard);
                    e.Handled = true;
                }
            };
            _commandButtons[item] = button;
            _commands.Children.Add(button);
        }
    }

    private void OnCommandHovered(RibbonApplicationMenuItem item)
    {
        if (_search.Text.Length > 0)
        {
            return;
        }

        if (item.Items.Count > 0)
        {
            ShowSubItems(item);
        }
        else if (_shownItem is not null)
        {
            ShowDefaultPane();
        }
    }

    private void ShowSubItems(RibbonApplicationMenuItem item)
    {
        _shownItem = item;
        _paneHeader.Text = item.Label ?? string.Empty;
        _pane.Children.Clear();
        foreach (var sub in item.Items)
        {
            var button = CreateRow(sub.Icon ?? item.Icon, sub.Label, sub.Description, 32, false);
            button.IsEnabled = sub.IsEnabled;
            AutomationProperties.SetName(button, sub.Label ?? string.Empty);
            var captured = sub;
            button.Click += (_, _) => Invoke(captured);
            button.KeyDown += (_, e) => BackToCommands(e, item);
            _pane.Children.Add(button);
        }

        foreach (var (key, commandButton) in _commandButtons)
        {
            SetSelected(commandButton, ReferenceEquals(key, item));
        }
    }

    private void BackToCommands(KeyRoutedEventArgs e, RibbonApplicationMenuItem owner)
    {
        if (e.Key == VirtualKey.Left && _commandButtons.TryGetValue(owner, out var button))
        {
            button.Focus(FocusState.Keyboard);
            e.Handled = true;
        }
    }

    private void OnRecentItemsChanged()
    {
        foreach (var item in RecentItems)
        {
            item.Changed -= OnRecentChanged;
            item.Changed += OnRecentChanged;
        }

        if (_shownItem is null && _search.Text.Length == 0)
        {
            ShowDefaultPane();
        }
    }

    private void OnRecentChanged(object? sender, EventArgs e)
    {
        if (_shownItem is null && _search.Text.Length == 0)
        {
            ShowDefaultPane();
        }
    }

    private void ShowDefaultPane()
    {
        _shownItem = null;
        _paneHeader.Text = RecentHeader ?? RibbonStrings.Current.RecentDocuments;
        _pane.Children.Clear();
        foreach (var recent in RecentItems.OrderByDescending(r => r.IsPinned))
        {
            _pane.Children.Add(CreateRecentRow(recent));
        }

        foreach (var commandButton in _commandButtons.Values)
        {
            SetSelected(commandButton, false);
        }
    }

    private FrameworkElement CreateRecentRow(RibbonApplicationMenuRecentItem recent)
    {
        var button = CreateRow(recent.Icon ?? "", recent.Title, recent.Path, 24, false);
        AutomationProperties.SetName(button, recent.Title ?? string.Empty);
        button.Click += (_, _) => Invoke(recent);
        var pin = new ToggleButton
        {
            IsChecked = recent.IsPinned,
            Content = new FontIcon { Glyph = recent.IsPinned ? "" : "", FontSize = 12 },
            Width = 28,
            Height = 28,
            Padding = new Thickness(0),
            MinWidth = 0,
            MinHeight = 0,
            BorderThickness = new Thickness(0),
            Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent),
            VerticalAlignment = VerticalAlignment.Center,
        };
        var pinText = recent.IsPinned ? RibbonStrings.Current.UnpinPanel : RibbonStrings.Current.PinPanel;
        AutomationProperties.SetName(pin, pinText);
        ToolTipService.SetToolTip(pin, pinText);
        pin.Click += (_, _) => recent.IsPinned = pin.IsChecked == true;
        var row = new Grid { ColumnDefinitions = { new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }, new ColumnDefinition { Width = GridLength.Auto } } };
        row.Children.Add(button);
        Grid.SetColumn(pin, 1);
        row.Children.Add(pin);
        return row;
    }

    private Button CreateRow(object? icon, string? title, string? subtitle, double iconSize, bool chevron)
    {
        var text = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Spacing = 1 };
        var titleBlock = new TextBlock { Text = title ?? string.Empty, FontSize = 13, TextTrimming = TextTrimming.CharacterEllipsis };
        text.Children.Add(titleBlock);
        if (!string.IsNullOrWhiteSpace(subtitle))
        {
            var sub = new TextBlock { Text = subtitle, FontSize = 11, TextWrapping = TextWrapping.Wrap, MaxLines = 2, TextTrimming = TextTrimming.CharacterEllipsis };
            RibbonTheme.SetThemeBrush(sub, TextBlock.ForegroundProperty, "RibbonSecondaryForegroundBrush");
            text.Children.Add(sub);
        }

        var grid = new Grid
        {
            ColumnSpacing = 10,
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = new GridLength(iconSize) },
                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                new ColumnDefinition { Width = GridLength.Auto },
            },
        };
        var presenter = new RibbonIconPresenter { Icon = icon, IconSize = iconSize, VerticalAlignment = VerticalAlignment.Center };
        RibbonTheme.SetThemeBrush(presenter, RibbonIconPresenter.ForegroundProperty, "RibbonIconBrush");
        grid.Children.Add(presenter);
        Grid.SetColumn(text, 1);
        grid.Children.Add(text);
        if (chevron)
        {
            var arrow = new FontIcon { Glyph = "", FontSize = 10, VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(arrow, 2);
            grid.Children.Add(arrow);
        }

        var button = new Button
        {
            Content = grid,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            Padding = new Thickness(10, 4, 10, 4),
            MinHeight = 36,
        };
        if (Application.Current.Resources.TryGetValue("RibbonMenuItemButtonStyle", out var style) && style is Style buttonStyle)
        {
            button.Style = buttonStyle;
        }

        return button;
    }

    private static void SetSelected(Button button, bool selected)
    {
        if (selected)
        {
            RibbonTheme.SetThemeBrush(button, Control.BackgroundProperty, "RibbonItemHoverBrush");
        }
        else
        {
            button.ClearValue(Control.BackgroundProperty);
        }
    }

    private void BuildFooter()
    {
        _footer.Children.Clear();
        foreach (var item in FooterItems)
        {
            if (item is FrameworkElement { Parent: Panel parent } && !ReferenceEquals(parent, _footer))
            {
                parent.Children.Remove(item);
            }

            if (item is Button button)
            {
                button.Click -= OnFooterClick;
                button.Click += OnFooterClick;
            }

            _footer.Children.Add(item);
        }
    }

    private void OnFooterClick(object sender, RoutedEventArgs e) => CloseHost();

    private void OnSearchTextChanged()
    {
        var query = _search.Text;
        if (string.IsNullOrWhiteSpace(query))
        {
            ShowDefaultPane();
            return;
        }

        _shownItem = null;
        _pane.Children.Clear();
        _paneHeader.Text = RibbonStrings.Current.SearchActions;
        var results = SearchMenu(query);
        foreach (var result in results)
        {
            var entry = result.Entry;
            var button = CreateRow(entry.Target is IRibbonItem ri ? ri.Icon : (entry.Target as RibbonApplicationMenuItem)?.Icon, entry.Label, entry.Path, 20, false);
            button.IsEnabled = entry.IsEnabled;
            AutomationProperties.SetName(button, entry.Label);
            button.Click += (_, _) => ExecuteSearchResult(entry);
            _pane.Children.Add(button);
        }

        if (results.Count == 0)
        {
            var none = new TextBlock { Text = RibbonStrings.Current.SearchNoResults, Margin = new Thickness(10, 4, 10, 4), Opacity = 0.7 };
            _pane.Children.Add(none);
        }
    }

    /// <summary>Searches the menu's own commands and the ribbon (when <see cref="Ribbon"/> is set).</summary>
    public IReadOnlyList<RibbonSearchResult> SearchMenu(string query)
    {
        var engine = new RibbonSearchEngine();
        var entries = new List<RibbonSearchEntry>();
        foreach (var item in Items)
        {
            AddMenuEntries(entries, item, null);
        }

        engine.SetEntries(entries);
        var menuResults = engine.Search(query, 6);
        var ribbonResults = Ribbon?.Search(query, 10) ?? [];
        return menuResults.Concat(ribbonResults).OrderByDescending(r => r.Score).Take(12).ToArray();
    }

    private static void AddMenuEntries(List<RibbonSearchEntry> entries, RibbonApplicationMenuItem item, string? path)
    {
        if (!string.IsNullOrEmpty(item.Label) && item.Items.Count == 0)
        {
            entries.Add(new RibbonSearchEntry("appmenu/" + (item.Id ?? (path + "/" + item.Label)), item.Label!, path ?? RibbonStrings.Current.File, item.Description, Target: item, IsEnabled: item.IsEnabled));
        }

        foreach (var sub in item.Items)
        {
            AddMenuEntries(entries, sub, item.Label);
        }
    }

    private void ExecuteSearchResult(RibbonSearchEntry entry)
    {
        if (!entry.IsEnabled)
        {
            return;
        }

        if (entry.Target is RibbonApplicationMenuItem item)
        {
            Invoke(item);
            return;
        }

        CloseHost();
        Ribbon?.ExecuteSearchEntry(entry);
    }

    private void OnSearchKeyDown(object sender, KeyRoutedEventArgs e)
    {
        switch (e.Key)
        {
            case VirtualKey.Enter when _search.Text.Length > 0:
                if (_pane.Children.OfType<Button>().FirstOrDefault(b => b.IsEnabled) is { } first)
                {
                    new Microsoft.UI.Xaml.Automation.Peers.ButtonAutomationPeer(first).Invoke();
                }

                e.Handled = true;
                break;
            case VirtualKey.Down:
                (_search.Text.Length > 0 ? _pane.Children.OfType<Control>().FirstOrDefault() : _commands.Children.OfType<Control>().FirstOrDefault())?.Focus(FocusState.Keyboard);
                e.Handled = true;
                break;
        }
    }

    private void CloseHost()
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
}
