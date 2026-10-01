using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using RibbonSpace.Localization;
using RibbonSpace.Search;
using Windows.Foundation;
using Windows.System;

namespace RibbonSpace.Controls;

/// <summary>Builds the result list shared by <see cref="RibbonSearchBox"/> and <see cref="RibbonCommandPalette"/>.</summary>
internal sealed class RibbonSearchResultsList
{
    private readonly StackPanel _panel = new() { Spacing = 0, Padding = new Thickness(4) };
    private readonly List<(Button Button, RibbonSearchEntry Entry)> _rows = [];
    private int _highlight = -1;

    public RibbonSearchResultsList()
    {
        Root = new ScrollViewer { Content = _panel, MaxHeight = 420, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
        Root.ActualThemeChanged += (_, _) => Highlight(_highlight);
    }

    public event EventHandler<RibbonSearchEntry>? Invoked;

    public FrameworkElement Root { get; }

    /// <summary>Element announcing the highlighted row to screen readers (the search text box keeps focus).</summary>
    public UIElement? Announcer { get; set; }

    public int Count => _rows.Count;

    /// <summary>Highlighted entry (never a disabled one).</summary>
    public RibbonSearchEntry? Highlighted => _highlight >= 0 && _highlight < _rows.Count && _rows[_highlight].Entry.IsEnabled ? _rows[_highlight].Entry : null;

    public void Show(IReadOnlyList<RibbonSearchResult> results, string? query)
    {
        _panel.Children.Clear();
        _rows.Clear();
        var strings = RibbonStrings.Current;
        if (results.Count == 0)
        {
            _panel.Children.Add(new TextBlock { Text = string.IsNullOrWhiteSpace(query) ? strings.SearchRecent : strings.SearchNoResults, Margin = new Thickness(8, 6, 8, 6), Opacity = 0.7 });
            _highlight = -1;
            return;
        }

        _panel.Children.Add(new TextBlock { Text = string.IsNullOrWhiteSpace(query) ? strings.SearchRecent : strings.SearchActions, FontSize = 12, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Margin = new Thickness(8, 4, 8, 4) });
        var position = 0;
        foreach (var result in results)
        {
            var entry = result.Entry;
            var icon = entry.Target switch
            {
                IRibbonItem item => item.Icon,
                RibbonBackstageItem b => b.Icon,
                Commands.RibbonCommandDescriptor d => d.Icon,
                _ => null,
            };
            var grid = new Grid { ColumnSpacing = 10 };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(20) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var presenter = new Primitives.RibbonIconPresenter { Icon = icon ?? "", IconSize = 16 };
            RibbonTheme.SetThemeBrush(presenter, Primitives.RibbonIconPresenter.ForegroundProperty, "RibbonIconBrush");
            grid.Children.Add(presenter);
            var text = new StackPanel();
            text.Children.Add(new TextBlock { Text = entry.Label, TextTrimming = TextTrimming.CharacterEllipsis });
            if (!string.IsNullOrEmpty(entry.Path))
            {
                text.Children.Add(new TextBlock { Text = entry.Path, FontSize = 11, Opacity = 0.65, TextTrimming = TextTrimming.CharacterEllipsis });
            }

            Grid.SetColumn(text, 1);
            grid.Children.Add(text);
            if (!string.IsNullOrEmpty(entry.Shortcut))
            {
                var shortcut = new TextBlock { Text = entry.Shortcut, FontSize = 11, Opacity = 0.65, VerticalAlignment = VerticalAlignment.Center };
                Grid.SetColumn(shortcut, 2);
                grid.Children.Add(shortcut);
            }

            var button = new Button { Content = grid, IsEnabled = entry.IsEnabled, HorizontalAlignment = HorizontalAlignment.Stretch, HorizontalContentAlignment = HorizontalAlignment.Stretch, IsTabStop = false };
            if (Application.Current.Resources.TryGetValue("RibbonMenuItemButtonStyle", out var style))
            {
                button.Style = (Style)style;
            }

            AutomationProperties.SetName(button, entry.Label);
            AutomationProperties.SetPositionInSet(button, ++position);
            AutomationProperties.SetSizeOfSet(button, results.Count);
            if (!string.IsNullOrEmpty(entry.Path))
            {
                AutomationProperties.SetHelpText(button, entry.Path);
            }

            var captured = entry;
            button.Click += (_, _) =>
            {
                if (captured.IsEnabled)
                {
                    Invoked?.Invoke(this, captured);
                }
            };
            _rows.Add((button, entry));
            _panel.Children.Add(button);
        }

        Highlight(_rows.FindIndex(r => r.Entry.IsEnabled));
    }

    /// <summary>Moves the highlight to the next / previous enabled row.</summary>
    public void Move(int delta)
    {
        if (_rows.Count == 0 || delta == 0)
        {
            return;
        }

        var step = Math.Sign(delta);
        for (var index = _highlight + step; index >= 0 && index < _rows.Count; index += step)
        {
            if (_rows[index].Entry.IsEnabled)
            {
                Highlight(index);
                return;
            }
        }
    }

    private void Highlight(int index)
    {
        var changed = index != _highlight;
        _highlight = _rows.Count == 0 ? -1 : index;
        var brush = RibbonTheme.GetBrush(Root, "RibbonItemHoverBrush");
        for (var i = 0; i < _rows.Count; i++)
        {
            _rows[i].Button.Background = i == _highlight && brush is not null ? brush : new SolidColorBrush(Microsoft.UI.Colors.Transparent);
        }

        if (_highlight >= 0 && _highlight < _rows.Count)
        {
            _rows[_highlight].Button.StartBringIntoView();
            if (changed)
            {
                Announce(_rows[_highlight].Entry);
            }
        }
    }

    private void Announce(RibbonSearchEntry entry)
    {
        if (Announcer is null)
        {
            return;
        }

        try
        {
            var text = string.IsNullOrEmpty(entry.Path) ? entry.Label : $"{entry.Label}, {entry.Path}";
            FrameworkElementAutomationPeer.FromElement(Announcer)?.RaiseNotificationEvent(AutomationNotificationKind.ItemAdded, AutomationNotificationProcessing.MostRecent, text, "RibbonSearchHighlight");
        }
        catch (Exception ex) when (ex is NotImplementedException or NotSupportedException or InvalidOperationException or System.Runtime.InteropServices.COMException)
        {
            // Automation notifications are not available on every platform.
        }
    }
}

/// <summary>
/// Microsoft Search style command search box ("Search (Alt+Q)") for the title bar or tab row. Searches the linked
/// <see cref="Ribbon"/> (all tabs, contextual tabs, backstage, catalog) and executes the chosen command. When the box
/// is hidden (narrow title bars), Alt+Q opens the <see cref="RibbonCommandPalette"/> instead.
/// </summary>
public partial class RibbonSearchBox : Control
{
    /// <summary>Identifies <see cref="Ribbon"/>.</summary>
    public static readonly DependencyProperty RibbonProperty = DependencyProperty.Register(nameof(Ribbon), typeof(Ribbon), typeof(RibbonSearchBox), new PropertyMetadata(null, (d, e) => ((RibbonSearchBox)d).OnRibbonChanged((Ribbon?)e.OldValue, (Ribbon?)e.NewValue)));

    /// <summary>Identifies <see cref="PlaceholderText"/>.</summary>
    public static readonly DependencyProperty PlaceholderTextProperty = DependencyProperty.Register(nameof(PlaceholderText), typeof(string), typeof(RibbonSearchBox), new PropertyMetadata(null, (d, _) => ((RibbonSearchBox)d).UpdatePlaceholder()));

    /// <summary>Identifies <see cref="MaxResults"/>.</summary>
    public static readonly DependencyProperty MaxResultsProperty = DependencyProperty.Register(nameof(MaxResults), typeof(int), typeof(RibbonSearchBox), new PropertyMetadata(10));

    private readonly RibbonSearchResultsList _results = new();
    private readonly List<RibbonSearchEntry> _queryEntries = [];
    private TextBox? _textBox;
    private Popup? _popup;
    private string? _ignoredText;
    private WeakReference<UIElement>? _returnFocus;

    /// <summary>Creates a search box.</summary>
    public RibbonSearchBox()
    {
        DefaultStyleKey = typeof(RibbonSearchBox);
        RibbonTheme.EnsureResources();
        _results.Invoked += (_, entry) => Execute(entry);
        IsTabStop = false;
    }

    /// <summary>Raised to let the application add results (help, documents, people...) for the current query.</summary>
    public event EventHandler<RibbonSearchQueryEventArgs>? QuerySubmitting;

    /// <summary>Raised after a result was executed.</summary>
    public event EventHandler<RibbonSearchEntry>? ResultExecuted;

    /// <summary>Ribbon searched and driven by this box.</summary>
    public Ribbon? Ribbon { get => (Ribbon?)GetValue(RibbonProperty); set => SetValue(RibbonProperty, value); }

    /// <summary>Placeholder (defaults to "Search (Alt+Q)").</summary>
    public string? PlaceholderText { get => (string?)GetValue(PlaceholderTextProperty); set => SetValue(PlaceholderTextProperty, value); }

    /// <summary>Maximum results.</summary>
    public int MaxResults { get => (int)GetValue(MaxResultsProperty); set => SetValue(MaxResultsProperty, value); }

    /// <summary>True while results are shown.</summary>
    public bool IsResultsOpen => _popup?.IsOpen == true;

    /// <inheritdoc />
    protected override void OnApplyTemplate()
    {
        if (_textBox is not null)
        {
            _textBox.TextChanged -= OnTextChanged;
            _textBox.KeyDown -= OnKeyDown;
            _textBox.GotFocus -= OnGotFocus;
            _textBox.GettingFocus -= OnGettingFocus;
        }

        base.OnApplyTemplate();
        _textBox = GetTemplateChild("PART_TextBox") as TextBox;
        _results.Announcer = _textBox;
        if (_textBox is not null)
        {
            UpdatePlaceholder();
            _textBox.TextChanged += OnTextChanged;
            _textBox.KeyDown += OnKeyDown;
            _textBox.GotFocus += OnGotFocus;
            _textBox.GettingFocus += OnGettingFocus;
            AutomationProperties.SetName(_textBox, RibbonStrings.Current.Search);
        }
    }

    private void UpdatePlaceholder()
    {
        if (_textBox is not null)
        {
            _textBox.PlaceholderText = PlaceholderText ?? RibbonStrings.Current.SearchPlaceholder;
        }
    }

    private void OnRibbonChanged(Ribbon? oldValue, Ribbon? newValue)
    {
        if (oldValue is not null)
        {
            oldValue.SearchRequested -= OnSearchRequested;
            RemoveQueryEntries(oldValue);
        }

        if (newValue is not null)
        {
            newValue.SearchRequested += OnSearchRequested;
        }
    }

    private void OnSearchRequested(object? sender, EventArgs e)
    {
        if (Visibility != Visibility.Visible || !IsLoaded || ActualWidth <= 0 || _textBox is null)
        {
            // Hidden in narrow windows: fall back to the command palette.
            if (Ribbon is { XamlRoot: not null } ribbon)
            {
                RibbonCommandPalette.Show(ribbon);
            }

            return;
        }

        FocusSearch();
    }

    /// <summary>Focuses the box and shows recent commands.</summary>
    public void FocusSearch()
    {
        if (_textBox is null)
        {
            return;
        }

        _textBox.Focus(FocusState.Keyboard);
        _textBox.SelectAll();
        ShowResults();
    }

    private void OnGettingFocus(UIElement sender, GettingFocusEventArgs args)
    {
        // Remember where focus came from so Esc can return it (Alt+Q from the document, click...).
        if (args.OldFocusedElement is UIElement old && !IsInside(old))
        {
            _returnFocus = new WeakReference<UIElement>(old);
        }
    }

    private bool IsInside(DependencyObject element)
    {
        for (var current = element; current is not null; current = VisualTreeHelper.GetParent(current))
        {
            if (ReferenceEquals(current, this))
            {
                return true;
            }
        }

        return false;
    }

    private void OnGotFocus(object sender, RoutedEventArgs e) => ShowResults();

    private void OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_textBox is null)
        {
            return;
        }

        // TextChanged is raised asynchronously: ignore the reset done after executing a result.
        if (_ignoredText is not null && _textBox.Text == _ignoredText)
        {
            _ignoredText = null;
            return;
        }

        _ignoredText = null;
        // Typing (focused) or a programmatic change while the results are showing refreshes them.
        if (_textBox.FocusState != FocusState.Unfocused || IsResultsOpen)
        {
            ShowResults();
        }
    }

    private void OnKeyDown(object sender, KeyRoutedEventArgs e)
    {
        switch (e.Key)
        {
            case VirtualKey.Down when !IsResultsOpen:
                ShowResults();
                e.Handled = true;
                break;
            case VirtualKey.Down:
                _results.Move(1);
                e.Handled = true;
                break;
            case VirtualKey.Up when IsResultsOpen:
                _results.Move(-1);
                e.Handled = true;
                break;
            case VirtualKey.Enter when IsResultsOpen:
                if (_results.Highlighted is { IsEnabled: true } entry)
                {
                    Execute(entry);
                }

                e.Handled = true;
                break;
            case VirtualKey.Escape:
                Close();
                ReturnFocus();
                e.Handled = true;
                break;
        }
    }

    private void ReturnFocus()
    {
        if (_returnFocus?.TryGetTarget(out var target) == true && target is { XamlRoot: not null } && !IsInside(target))
        {
            target.Focus(FocusState.Keyboard);
        }
        else
        {
            Ribbon?.SelectedTab?.HeaderElement?.Focus(FocusState.Keyboard);
        }

        _returnFocus = null;
    }

    private void ShowResults()
    {
        if (Ribbon is not { } ribbon || XamlRoot is null || _textBox is null)
        {
            return;
        }

        var query = _textBox.Text;
        var args = new RibbonSearchQueryEventArgs(query);
        QuerySubmitting?.Invoke(this, args);

        // Entries contributed for a query only live while that query is shown.
        RemoveQueryEntries(ribbon);
        foreach (var extra in args.AdditionalEntries)
        {
            if (!ribbon.AdditionalSearchEntries.Any(e => e.Id == extra.Id))
            {
                ribbon.AdditionalSearchEntries.Add(extra);
                _queryEntries.Add(extra);
            }
        }

        _results.Show(ribbon.Search(query, MaxResults), query);
        if (_popup is null)
        {
            var chrome = new Border { Child = _results.Root, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(8) };
            RibbonTheme.SetThemeBrush(chrome, Border.BackgroundProperty, "RibbonPopupBackgroundBrush");
            RibbonTheme.SetThemeBrush(chrome, Border.BorderBrushProperty, "RibbonPopupBorderBrush");
            _popup = new Popup { Child = chrome, IsLightDismissEnabled = true };
            _popup.Closed += (_, _) =>
            {
                if (Ribbon is { } current)
                {
                    RemoveQueryEntries(current);
                }
            };
        }

        if (_popup.Child is FrameworkElement child)
        {
            child.RequestedTheme = ActualTheme;
            child.Width = Math.Max(ActualWidth, 360);
        }

        Primitives.RibbonPopupPlacement.PlaceBelow(_popup, this);
        _popup.IsOpen = true;
    }

    private void RemoveQueryEntries(Ribbon ribbon)
    {
        foreach (var entry in _queryEntries)
        {
            ribbon.AdditionalSearchEntries.Remove(entry);
        }

        _queryEntries.Clear();
    }

    private void Execute(RibbonSearchEntry entry)
    {
        if (!entry.IsEnabled)
        {
            return;
        }

        Close();
        if (_textBox is not null && _textBox.Text.Length > 0)
        {
            _ignoredText = string.Empty;
            _textBox.Text = string.Empty;
        }

        _returnFocus = null;
        Ribbon?.ExecuteSearchEntry(entry);
        ResultExecuted?.Invoke(this, entry);
    }

    /// <summary>Closes the results.</summary>
    public void Close()
    {
        if (_popup is { IsOpen: true })
        {
            _popup.IsOpen = false;
        }
    }
}

/// <summary>Arguments of <see cref="RibbonSearchBox.QuerySubmitting"/>.</summary>
public sealed class RibbonSearchQueryEventArgs(string query) : EventArgs
{
    /// <summary>Query text.</summary>
    public string Query { get; } = query;

    /// <summary>Entries to add for this query (targets may be <see cref="Action"/>s); removed when the query changes.</summary>
    public List<RibbonSearchEntry> AdditionalEntries { get; } = [];
}

/// <summary>
/// Command palette (VS Code "Ctrl+Shift+P" / Figma "Ctrl+K"): a centered, modal fuzzy command search. Tab cycles
/// inside the palette and focus returns to the previously focused element when it closes.
/// </summary>
public static class RibbonCommandPalette
{
    /// <summary>Shows the palette for a ribbon.</summary>
    public static void Show(Ribbon ribbon)
    {
        ArgumentNullException.ThrowIfNull(ribbon);
        Show(ribbon.XamlRoot, q => ribbon.Search(q, 16), e => ribbon.ExecuteSearchEntry(e), ribbon.ActualTheme);
    }

    /// <summary>Shows the palette for arbitrary entries (catalog-only apps without a ribbon).</summary>
    public static void Show(XamlRoot? xamlRoot, IEnumerable<RibbonSearchEntry> entries, Action<RibbonSearchEntry> execute, ElementTheme theme = ElementTheme.Default)
    {
        ArgumentNullException.ThrowIfNull(execute);
        var engine = new RibbonSearchEngine();
        engine.SetEntries(entries);
        Show(xamlRoot, q => engine.Search(q, 16), e =>
        {
            engine.MarkUsed(e.Id);
            execute(e);
        }, theme);
    }

    private static void Show(XamlRoot? xamlRoot, Func<string, IReadOnlyList<RibbonSearchResult>> search, Action<RibbonSearchEntry> execute, ElementTheme theme)
    {
        if (xamlRoot is null)
        {
            return;
        }

        RibbonTheme.EnsureResources();
        var previousFocus = FocusManager.GetFocusedElement(xamlRoot) as UIElement;
        var results = new RibbonSearchResultsList();
        var box = new TextBox { PlaceholderText = RibbonStrings.Current.CommandPalette, Margin = new Thickness(8, 8, 8, 4) };
        AutomationProperties.SetName(box, RibbonStrings.Current.CommandPalette);
        results.Announcer = box;
        var panel = new StackPanel { Children = { box, results.Root } };
        var chrome = new Border
        {
            Child = panel,
            Width = Math.Max(0, Math.Min(600, xamlRoot.Size.Width - 40)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            RequestedTheme = theme,
            TabFocusNavigation = KeyboardNavigationMode.Cycle,
        };
        RibbonTheme.SetThemeBrush(chrome, Border.BackgroundProperty, "RibbonPopupBackgroundBrush");
        RibbonTheme.SetThemeBrush(chrome, Border.BorderBrushProperty, "RibbonPopupBorderBrush");
        var popup = new Popup { Child = chrome, IsLightDismissEnabled = true, XamlRoot = xamlRoot };
        popup.HorizontalOffset = Math.Max(0, (xamlRoot.Size.Width - chrome.Width) / 2);
        popup.VerticalOffset = 72;
        var executing = false;
        popup.Closed += (_, _) =>
        {
            // Executing a command may move focus on purpose; otherwise restore it.
            if (!executing && previousFocus is { XamlRoot: not null })
            {
                previousFocus.Focus(FocusState.Programmatic);
            }
        };
        void Run(RibbonSearchEntry entry)
        {
            if (!entry.IsEnabled)
            {
                return;
            }

            if (previousFocus is { XamlRoot: not null })
            {
                previousFocus.Focus(FocusState.Programmatic);
            }

            executing = true;
            popup.IsOpen = false;
            execute(entry);
        }

        results.Invoked += (_, e) => Run(e);
        box.TextChanged += (_, _) => results.Show(search(box.Text), box.Text);
        box.KeyDown += (_, e) =>
        {
            switch (e.Key)
            {
                case VirtualKey.Down: results.Move(1); e.Handled = true; break;
                case VirtualKey.Up: results.Move(-1); e.Handled = true; break;
                case VirtualKey.Enter:
                    if (results.Highlighted is { IsEnabled: true } entry)
                    {
                        Run(entry);
                    }

                    e.Handled = true;
                    break;
                case VirtualKey.Escape: popup.IsOpen = false; e.Handled = true; break;
            }
        };
        results.Show(search(string.Empty), string.Empty);
        popup.IsOpen = true;
        box.Focus(FocusState.Programmatic);
    }
}
