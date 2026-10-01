using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using RibbonSpace.Localization;
using RibbonSpace.State;

namespace RibbonSpace.Controls;

/// <summary>
/// Office "Customize the Ribbon" / "Quick Access Toolbar" dialog: show / hide / rename / reorder tabs and groups,
/// create custom tabs and groups with existing commands, edit the QAT, import / export and reset.
/// </summary>
public sealed partial class RibbonCustomizeDialog : ContentDialog
{
    private readonly Ribbon _ribbon;
    private readonly RibbonState _working;
    private readonly List<(string Id, string Label, string Path, object? Icon)> _commands;
    private readonly SelectableList _commandList = new();
    private readonly SelectableList _structureList = new();
    private readonly SelectableList _qatList = new();
    private readonly Grid _ribbonPage = new();
    private readonly Grid _qatPage = new();
    private readonly TextBox _search = new() { PlaceholderText = RibbonStrings.Current.Search };
    private readonly CheckBox _qatBelow = new() { Content = RibbonStrings.Current.ShowBelowRibbon };
    private readonly List<StructureRow> _rows = [];
    private readonly StackPanel _root = new() { Width = 860 };
    private XamlRoot? _sizedRoot;

    /// <summary>Creates the dialog.</summary>
    public RibbonCustomizeDialog(Ribbon ribbon, RibbonCustomizePage page)
    {
        _ribbon = ribbon ?? throw new ArgumentNullException(nameof(ribbon));
        _working = ribbon.GetState();
        _working.QuickAccessItemIds ??= [];
        _commands = CollectCommands();
        var strings = RibbonStrings.Current;
        Title = page == RibbonCustomizePage.Ribbon ? strings.CustomizeRibbon.TrimEnd('.') : strings.CustomizeQuickAccessToolbar;
        PrimaryButtonText = strings.Ok;
        CloseButtonText = strings.Cancel;
        DefaultButton = ContentDialogButton.Primary;
        Resources["ContentDialogMaxWidth"] = 960d;
        Resources["ContentDialogMaxHeight"] = 760d;
        PrimaryButtonClick += (_, _) => _ribbon.ApplyState(_working);

        var ribbonTab = new ToggleButton(strings.CustomizeRibbon.TrimEnd('.'));
        var qatTab = new ToggleButton(strings.QuickAccessToolbar);
        var header = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4, Margin = new Thickness(0, 0, 0, 10) };
        header.Children.Add(ribbonTab.Button);
        header.Children.Add(qatTab.Button);
        BuildRibbonPage();
        BuildQuickAccessPage();
        var pages = new Grid();
        pages.Children.Add(_ribbonPage);
        pages.Children.Add(_qatPage);
        void Show(RibbonCustomizePage p)
        {
            _ribbonPage.Visibility = p == RibbonCustomizePage.Ribbon ? Visibility.Visible : Visibility.Collapsed;
            _qatPage.Visibility = p == RibbonCustomizePage.QuickAccessToolbar ? Visibility.Visible : Visibility.Collapsed;
            ribbonTab.IsChecked = p == RibbonCustomizePage.Ribbon;
            qatTab.IsChecked = !ribbonTab.IsChecked;
        }

        ribbonTab.Button.Click += (_, _) => Show(RibbonCustomizePage.Ribbon);
        qatTab.Button.Click += (_, _) => Show(RibbonCustomizePage.QuickAccessToolbar);
        Show(page);
        _root.Children.Add(header);
        _root.Children.Add(pages);
        Content = _root;
        _structureList.Name = strings.MainTabs;
        _commandList.Name = strings.ChooseCommands;
        _qatList.Name = strings.QuickAccessToolbar;
        Opened += (_, _) => AttachSizing();
        Closed += (_, _) => DetachSizing();
        _search.TextChanged += (_, _) => FillCommands();
        FillCommands();
        FillStructure();
        FillQuickAccess();
    }

    private void AttachSizing()
    {
        DetachSizing();
        _sizedRoot = XamlRoot;
        if (_sizedRoot is not null)
        {
            _sizedRoot.Changed += OnXamlRootChanged;
        }

        UpdateWidth();
    }

    private void DetachSizing()
    {
        if (_sizedRoot is not null)
        {
            _sizedRoot.Changed -= OnXamlRootChanged;
            _sizedRoot = null;
        }
    }

    private void OnXamlRootChanged(XamlRoot sender, XamlRootChangedEventArgs args) => UpdateWidth();

    private void UpdateWidth()
    {
        // min(860, window width - dialog chrome margins)
        if (XamlRoot is { } root && root.Size.Width > 0)
        {
            _root.Width = Math.Max(320, Math.Min(860, root.Size.Width - 96));
        }
    }

    /// <summary>Working copy of the state (applied when OK is pressed).</summary>
    public RibbonState WorkingState => _working;

    private RibbonCustomization C => _working.Customization;

    private List<(string, string, string, object?)> CollectCommands()
    {
        var list = new List<(string, string, string, object?)>();
        foreach (var tab in _ribbon.Tabs.Where(t => !t.IsCustom))
        {
            foreach (var group in tab.Groups)
            {
                foreach (var element in group.GetAllItems())
                {
                    if (element is IRibbonItem { Id: { } id, Label: { } label, CanAddToQuickAccess: true } item && RibbonItemHelper.GetSourceItem(element) is null && list.All(c => c.Item1 != id))
                    {
                        list.Add((id, label.Replace('\n', ' '), $"{tab.Header} › {group.Header}", item.Icon));
                    }
                }
            }
        }

        return list.OrderBy(c => c.Item2, StringComparer.CurrentCultureIgnoreCase).ToList();
    }

    private void FillCommands()
    {
        var query = _search.Text;
        _commandList.Fill(_commands
            .Where(c => string.IsNullOrWhiteSpace(query) || c.Label.Contains(query, StringComparison.CurrentCultureIgnoreCase) || c.Path.Contains(query, StringComparison.CurrentCultureIgnoreCase))
            .Select(c => new ListRow(c.Id, c.Label, c.Path, c.Icon, 0, null)));
    }

    private Grid Columns(FrameworkElement left, FrameworkElement middle, FrameworkElement right, FrameworkElement buttons)
    {
        var grid = new Grid { ColumnSpacing = 12 };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(middle, 1);
        Grid.SetColumn(right, 2);
        Grid.SetColumn(buttons, 3);
        grid.Children.Add(left);
        grid.Children.Add(middle);
        grid.Children.Add(right);
        grid.Children.Add(buttons);
        return grid;
    }

    private static StackPanel Stack(params UIElement[] children)
    {
        var panel = new StackPanel { Spacing = 6, VerticalAlignment = VerticalAlignment.Center };
        foreach (var child in children)
        {
            panel.Children.Add(child);
        }

        return panel;
    }

    private static Button Command(string text, Action action)
    {
        var button = new Button { Content = text, HorizontalAlignment = HorizontalAlignment.Stretch, MinWidth = 110 };
        button.Click += (_, _) => action();
        return button;
    }

    private FrameworkElement Labeled(string label, FrameworkElement content)
    {
        var panel = new StackPanel { Spacing = 6 };
        panel.Children.Add(new TextBlock { Text = label, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
        panel.Children.Add(content);
        return panel;
    }

    private void BuildRibbonPage()
    {
        var strings = RibbonStrings.Current;
        var left = Labeled(strings.ChooseCommands, Stack(_search, _commandList.Root));
        var middle = Stack(Command(strings.Add, AddToCustomGroup), Command(strings.Remove, RemoveFromCustomGroup));
        var right = Labeled(strings.MainTabs, _structureList.Root);
        var buttons = Stack(
            Command(strings.MoveUp, () => MoveStructure(-1)),
            Command(strings.MoveDown, () => MoveStructure(1)),
            Command(strings.NewTab, NewTab),
            Command(strings.NewGroup, NewGroup),
            Command(strings.Rename, Rename),
            Command(strings.Reset, () =>
            {
                _working.Customization = new RibbonCustomization();
                FillStructure();
            }),
            Command(strings.ImportExport, ImportExport));
        _ribbonPage.Children.Add(Columns(left, middle, right, buttons));
        _structureList.ToggleChanged += OnVisibilityToggled;
    }

    private void BuildQuickAccessPage()
    {
        var strings = RibbonStrings.Current;
        var commands = new SelectableList();
        commands.Fill(_commands.Select(c => new ListRow(c.Id, c.Label, c.Path, c.Icon, 0, null)));
        var left = Labeled(strings.ChooseCommands, commands.Root);
        var middle = Stack(
            Command(strings.Add, () =>
            {
                if (commands.Selected is { } row && !_working.QuickAccessItemIds!.Contains(row.Id))
                {
                    _working.QuickAccessItemIds.Add(row.Id);
                    FillQuickAccess();
                }
            }),
            Command(strings.Remove, () =>
            {
                if (_qatList.Selected is { } row)
                {
                    _working.QuickAccessItemIds!.Remove(row.Id);
                    FillQuickAccess();
                }
            }));
        var right = Labeled(strings.QuickAccessToolbar, _qatList.Root);
        _qatBelow.IsChecked = _working.QuickAccessPosition == RibbonQuickAccessPosition.BelowRibbon;
        _qatBelow.Checked += (_, _) => _working.QuickAccessPosition = RibbonQuickAccessPosition.BelowRibbon;
        _qatBelow.Unchecked += (_, _) => _working.QuickAccessPosition = RibbonQuickAccessPosition.AboveRibbon;
        var buttons = Stack(
            Command(strings.MoveUp, () => MoveQuickAccess(-1)),
            Command(strings.MoveDown, () => MoveQuickAccess(1)),
            Command(strings.Reset, () =>
            {
                // The application defaults (captured when the ribbon first loaded), not the current QAT.
                _working.QuickAccessItemIds = (_ribbon.DefaultQuickAccessItemIds ?? _ribbon.GetQuickAccessItemIds()).ToList();
                _working.QuickAccessPosition = _ribbon.DefaultQuickAccessPosition;
                _working.IsQuickAccessVisible = true;
                _qatBelow.IsChecked = _working.QuickAccessPosition == RibbonQuickAccessPosition.BelowRibbon;
                FillQuickAccess();
            }),
            _qatBelow);
        _qatPage.Children.Add(Columns(left, middle, right, buttons));
    }

    private void FillQuickAccess()
    {
        var lookup = _commands.ToDictionary(c => c.Id, c => c);
        _qatList.Fill(_working.QuickAccessItemIds!.Select(id =>
        {
            if (lookup.TryGetValue(id, out var c))
            {
                return new ListRow(id, c.Label, c.Path, c.Icon, 0, null);
            }

            var own = _ribbon.QuickAccessToolBar?.Items.OfType<IRibbonItem>().FirstOrDefault(i => i.Id == id);
            return new ListRow(id, own?.Label ?? id, string.Empty, own?.Icon, 0, null);
        }));
    }

    private void MoveQuickAccess(int delta)
    {
        if (_qatList.Selected is not { } row)
        {
            return;
        }

        var ids = _working.QuickAccessItemIds!;
        var index = ids.IndexOf(row.Id);
        var target = Math.Clamp(index + delta, 0, ids.Count - 1);
        if (index < 0 || target == index)
        {
            return;
        }

        ids.RemoveAt(index);
        ids.Insert(target, row.Id);
        FillQuickAccess();
        _qatList.Select(row.Id);
    }

    private sealed record StructureRow(string Id, string Label, int Level, bool Hidden, bool IsCustom, string? TabId);

    private void FillStructure()
    {
        _rows.Clear();
        var tabs = _ribbon.Tabs.Where(t => !t.IsCustom && !t.IsContextual && t.EffectiveId is not null).ToList();
        var order = C.TabOrder;
        var orderedIds = tabs.Select(t => t.EffectiveId!).Concat(C.CustomTabs.Select(t => t.Id)).ToList();
        if (order.Count > 0)
        {
            orderedIds = orderedIds.OrderBy(id => order.IndexOf(id) is var i && i < 0 ? int.MaxValue : i).ToList();
        }

        foreach (var id in orderedIds)
        {
            var custom = C.CustomTabs.FirstOrDefault(t => t.Id == id);
            if (custom is not null)
            {
                _rows.Add(new StructureRow(custom.Id, custom.Label + " " + RibbonStrings.Current.CustomGroupSuffix, 0, C.HiddenTabIds.Contains(custom.Id), true, null));
                foreach (var g in custom.Groups)
                {
                    _rows.Add(new StructureRow(g.Id, g.Label, 1, C.HiddenGroupIds.Contains(g.Id), true, custom.Id));
                    foreach (var itemId in g.ItemIds)
                    {
                        _rows.Add(new StructureRow(g.Id + "/" + itemId, _commands.FirstOrDefault(c => c.Id == itemId).Label ?? itemId, 2, false, true, g.Id));
                    }
                }

                continue;
            }

            var tab = tabs.First(t => t.EffectiveId == id);
            _rows.Add(new StructureRow(id, C.Labels.GetValueOrDefault(id, tab.OriginalHeader ?? tab.Header ?? id), 0, C.HiddenTabIds.Contains(id), false, null));
            var builtInGroups = tab.Groups.Where(g => g.EffectiveId is not null && !_ribbonCustomGroupIds.Contains(g.EffectiveId!)).ToList();
            if (C.GroupOrder.TryGetValue(id, out var groupOrder) && groupOrder.Count > 0)
            {
                builtInGroups = builtInGroups.OrderBy(g => groupOrder.IndexOf(g.EffectiveId!) is var i && i < 0 ? int.MaxValue : i).ToList();
            }

            foreach (var group in builtInGroups)
            {
                _rows.Add(new StructureRow(group.EffectiveId!, C.Labels.GetValueOrDefault(group.EffectiveId!, group.OriginalHeader ?? group.Header ?? group.EffectiveId!), 1, C.HiddenGroupIds.Contains(group.EffectiveId!), false, id));
            }

            foreach (var g in C.CustomGroups.Where(g => g.TabId == id))
            {
                _rows.Add(new StructureRow(g.Id, g.Label + " " + RibbonStrings.Current.CustomGroupSuffix, 1, C.HiddenGroupIds.Contains(g.Id), true, id));
                foreach (var itemId in g.ItemIds)
                {
                    _rows.Add(new StructureRow(g.Id + "/" + itemId, _commands.FirstOrDefault(c => c.Id == itemId).Label ?? itemId, 2, false, true, g.Id));
                }
            }
        }

        var selected = _structureList.Selected?.Id;
        _structureList.Fill(_rows.Select(r => new ListRow(r.Id, r.Label, null, null, r.Level, r.IsCustom && r.Level == 2 ? null : !r.Hidden)));
        if (selected is not null)
        {
            _structureList.Select(selected);
        }
    }

    private HashSet<string> _ribbonCustomGroupIds => C.CustomGroups.Select(g => g.Id).Concat(C.CustomTabs.SelectMany(t => t.Groups).Select(g => g.Id)).ToHashSet();

    private StructureRow? SelectedRow => _structureList.Selected is { } s ? _rows.FirstOrDefault(r => r.Id == s.Id) : null;

    private void OnVisibilityToggled(object? sender, (string Id, bool Visible) e)
    {
        // Custom tabs and groups are hidden like built-in ones (by id); command rows have no check box.
        var row = _rows.FirstOrDefault(r => r.Id == e.Id);
        if (row is null || row.Level > 1)
        {
            return;
        }

        var list = row.Level == 0 ? C.HiddenTabIds : C.HiddenGroupIds;
        list.Remove(row.Id);
        if (!e.Visible)
        {
            list.Add(row.Id);
        }
    }

    private RibbonCustomGroup? FindCustomGroup(string id)
        => C.CustomGroups.FirstOrDefault(g => g.Id == id) ?? C.CustomTabs.SelectMany(t => t.Groups).FirstOrDefault(g => g.Id == id);

    private RibbonCustomGroup? TargetGroup()
    {
        var row = SelectedRow;
        return row switch
        {
            { Level: 1, IsCustom: true } => FindCustomGroup(row.Id),
            { Level: 2 } => FindCustomGroup(row.TabId!),
            _ => null,
        };
    }

    private void AddToCustomGroup()
    {
        if (_commandList.Selected is not { } command)
        {
            return;
        }

        var group = TargetGroup();
        if (group is null)
        {
            NewGroup();
            group = TargetGroup();
        }

        if (group is not null && !group.ItemIds.Contains(command.Id))
        {
            group.ItemIds.Add(command.Id);
            FillStructure();
        }
    }

    private void RemoveFromCustomGroup()
    {
        var row = SelectedRow;
        if (row is null)
        {
            return;
        }

        if (row.Level == 2 && FindCustomGroup(row.TabId!) is { } group)
        {
            group.ItemIds.Remove(row.Id[(row.TabId!.Length + 1)..]);
        }
        else if (row is { Level: 1, IsCustom: true })
        {
            C.CustomGroups.RemoveAll(g => g.Id == row.Id);
            foreach (var t in C.CustomTabs)
            {
                t.Groups.RemoveAll(g => g.Id == row.Id);
            }
        }
        else if (row is { Level: 0, IsCustom: true })
        {
            C.CustomTabs.RemoveAll(t => t.Id == row.Id);
            C.TabOrder.Remove(row.Id);
        }

        FillStructure();
    }

    private void NewTab()
    {
        var id = "custom.tab." + Guid.NewGuid().ToString("N")[..8];
        var groupId = "custom.group." + Guid.NewGuid().ToString("N")[..8];
        C.CustomTabs.Add(new RibbonCustomTab { Id = id, Label = RibbonStrings.Current.NewTab, Groups = [new RibbonCustomGroup { Id = groupId, Label = RibbonStrings.Current.NewGroup }] });
        FillStructure();
        _structureList.Select(groupId);
    }

    private void NewGroup()
    {
        var row = SelectedRow;
        var tabId = row switch
        {
            null => _ribbon.SelectedTab?.EffectiveId,
            { Level: 0 } => row.Id,
            { Level: 1 } => row.TabId,
            _ => _rows.FirstOrDefault(r => r.Id == row.TabId)?.TabId,
        };
        if (tabId is null)
        {
            return;
        }

        var group = new RibbonCustomGroup { Id = "custom.group." + Guid.NewGuid().ToString("N")[..8], Label = RibbonStrings.Current.NewGroup, TabId = tabId };
        if (C.CustomTabs.FirstOrDefault(t => t.Id == tabId) is { } customTab)
        {
            group.TabId = null;
            customTab.Groups.Add(group);
        }
        else
        {
            C.CustomGroups.Add(group);
        }

        FillStructure();
        _structureList.Select(group.Id);
    }

    private async void Rename()
    {
        var row = SelectedRow;
        if (row is null || row.Level == 2)
        {
            return;
        }

        var box = new TextBox { Text = row.Label.Replace(" " + RibbonStrings.Current.CustomGroupSuffix, string.Empty, StringComparison.Ordinal) };
        await RunNestedDialogAsync(async () =>
        {
            var dialog = new ContentDialog { Title = RibbonStrings.Current.Rename.TrimEnd('.'), Content = box, PrimaryButtonText = RibbonStrings.Current.Ok, CloseButtonText = RibbonStrings.Current.Cancel, DefaultButton = ContentDialogButton.Primary, XamlRoot = XamlRoot, RequestedTheme = ActualTheme };
            if (await dialog.ShowAsync() != ContentDialogResult.Primary || string.IsNullOrWhiteSpace(box.Text))
            {
                return;
            }

            if (row.IsCustom)
            {
                if (C.CustomTabs.FirstOrDefault(t => t.Id == row.Id) is { } tab)
                {
                    tab.Label = box.Text;
                }
                else if (FindCustomGroup(row.Id) is { } group)
                {
                    group.Label = box.Text;
                }
            }
            else
            {
                C.Labels[row.Id] = box.Text;
            }

            FillStructure();
        });
    }

    /// <summary>
    /// Hides this dialog (only one ContentDialog can be open), runs a nested dialog and shows this dialog again. Dialog
    /// failures (another dialog already open, window closing) are swallowed so the async void callers never crash.
    /// </summary>
    private async Task RunNestedDialogAsync(Func<Task> body)
    {
        var root = XamlRoot;
        try
        {
            var closed = new TaskCompletionSource();
            void OnClosed(ContentDialog sender, ContentDialogClosedEventArgs args) => closed.TrySetResult();
            Closed += OnClosed;
            Hide();
            await Task.WhenAny(closed.Task, Task.Delay(1000));
            Closed -= OnClosed;
            await body();
        }
        catch (Exception ex) when (ex is InvalidOperationException or COMException or ArgumentException or TaskCanceledException)
        {
            // Another dialog is open or the window is closing.
        }

        try
        {
            if (root is not null)
            {
                XamlRoot = root;
                await ShowAsync();
            }
        }
        catch (Exception ex) when (ex is InvalidOperationException or COMException or ArgumentException or TaskCanceledException)
        {
            // The window is closing.
        }
    }

    private void MoveStructure(int delta)
    {
        var row = SelectedRow;
        switch (row)
        {
            case null:
                return;
            case { Level: 0 }:
                MoveTab(delta);
                return;
            case { Level: 1, IsCustom: true }:
                MoveCustomGroup(row, delta);
                return;
            case { Level: 2 } when FindCustomGroup(row.TabId!) is { } group:
                var itemId = row.Id[(row.TabId!.Length + 1)..];
                if (Move(group.ItemIds, itemId, delta))
                {
                    FillStructure();
                    _structureList.Select(row.Id);
                }

                return;
        }

        if (row is { Level: 1, IsCustom: false, TabId: { } tabId })
        {
            // Built-in groups: reorder among the built-in groups of their tab (RibbonCustomization.GroupOrder).
            var ids = _rows.Where(r => r is { Level: 1, IsCustom: false } && r.TabId == tabId).Select(r => r.Id).ToList();
            if (Move(ids, row.Id, delta))
            {
                C.GroupOrder[tabId] = ids;
                FillStructure();
                _structureList.Select(row.Id);
            }
        }
    }

    private void MoveCustomGroup(StructureRow row, int delta)
    {
        if (C.CustomTabs.FirstOrDefault(t => t.Groups.Any(g => g.Id == row.Id)) is { } tab)
        {
            var ids = tab.Groups.Select(g => g.Id).ToList();
            if (Move(ids, row.Id, delta))
            {
                tab.Groups = ids.Select(id => tab.Groups.First(g => g.Id == id)).ToList();
            }
        }
        else
        {
            // Custom groups of a built-in tab: reorder among the custom groups of that tab.
            var siblings = C.CustomGroups.Where(g => g.TabId == row.TabId).ToList();
            var ids = siblings.Select(g => g.Id).ToList();
            if (!Move(ids, row.Id, delta))
            {
                return;
            }

            var others = C.CustomGroups.Where(g => g.TabId != row.TabId).ToList();
            C.CustomGroups = others.Concat(ids.Select(id => siblings.First(g => g.Id == id))).ToList();
        }

        FillStructure();
        _structureList.Select(row.Id);
    }

    private static bool Move(List<string> ids, string id, int delta)
    {
        var index = ids.IndexOf(id);
        var target = Math.Clamp(index + delta, 0, ids.Count - 1);
        if (index < 0 || target == index)
        {
            return false;
        }

        ids.RemoveAt(index);
        ids.Insert(target, id);
        return true;
    }

    private void MoveTab(int delta)
    {
        var row = SelectedRow;
        if (row is not { Level: 0 })
        {
            return;
        }

        var ids = _rows.Where(r => r.Level == 0).Select(r => r.Id).ToList();
        var index = ids.IndexOf(row.Id);
        var target = Math.Clamp(index + delta, 0, ids.Count - 1);
        if (target == index)
        {
            return;
        }

        ids.RemoveAt(index);
        ids.Insert(target, row.Id);
        C.TabOrder = ids;
        FillStructure();
        _structureList.Select(row.Id);
    }

    private async void ImportExport()
    {
        var box = new TextBox { Text = RibbonStateSerializer.Serialize(_working), AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, Height = 360, Width = 560, FontFamily = new FontFamily("Consolas,Menlo,monospace") };
        await RunNestedDialogAsync(async () =>
        {
            var dialog = new ContentDialog { Title = RibbonStrings.Current.ImportExport, Content = box, PrimaryButtonText = RibbonStrings.Current.Ok, CloseButtonText = RibbonStrings.Current.Cancel, XamlRoot = XamlRoot, RequestedTheme = ActualTheme };
            if (await dialog.ShowAsync() != ContentDialogResult.Primary)
            {
                return;
            }

            if (RibbonStateSerializer.Deserialize(box.Text) is not { } imported)
            {
                var error = new ContentDialog { Title = RibbonStrings.Current.ImportExport, Content = new TextBlock { Text = InvalidImportMessage ?? RibbonStrings.Current.InvalidImport, TextWrapping = TextWrapping.Wrap }, CloseButtonText = RibbonStrings.Current.Ok, XamlRoot = XamlRoot, RequestedTheme = ActualTheme };
                await error.ShowAsync();
                return;
            }

            _working.Customization = imported.Customization;
            _working.QuickAccessItemIds = imported.QuickAccessItemIds ?? _working.QuickAccessItemIds;
            _working.QuickAccessPosition = imported.QuickAccessPosition;
            _qatBelow.IsChecked = _working.QuickAccessPosition == RibbonQuickAccessPosition.BelowRibbon;
            FillStructure();
            FillQuickAccess();
        });
    }

    /// <summary>Overrides the message shown when imported customization text is not valid (default: <c>RibbonStrings.InvalidImport</c>).</summary>
    public static string? InvalidImportMessage { get; set; }

    private sealed class ToggleButton
    {
        public ToggleButton(string text)
        {
            Button = new Button { Content = text, Padding = new Thickness(12, 6, 12, 6) };
            Button.ActualThemeChanged += (_, _) => IsChecked = IsChecked;
        }

        public Button Button { get; }

        public bool IsChecked
        {
            get => Button.Tag is true;
            set
            {
                Button.Tag = value;
                Button.FontWeight = value ? Microsoft.UI.Text.FontWeights.SemiBold : Microsoft.UI.Text.FontWeights.Normal;
                Button.BorderBrush = value ? RibbonTheme.GetBrush(Button, "RibbonAccentBrush") : null;
                Button.BorderThickness = new Thickness(0, 0, 0, value ? 2 : 0);
            }
        }
    }
}

/// <summary>Row of a <see cref="SelectableList"/>.</summary>
internal sealed record ListRow(string Id, string Label, string? Detail, object? Icon, int Level, bool? IsChecked);

/// <summary>
/// Simple selectable list with optional check boxes and indentation (used by the customization dialog). Up / Down /
/// Home / End move the selection; the selected row is focused and announced to screen readers.
/// </summary>
internal sealed class SelectableList
{
    private readonly StackPanel _panel = new();
    private readonly List<(ListRow Row, Button Button)> _items = [];

    public SelectableList()
    {
        Root = new Border
        {
            Height = 380,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Child = new ScrollViewer { Content = _panel, VerticalScrollBarVisibility = ScrollBarVisibility.Auto },
        };
        RibbonTheme.SetThemeBrush(Root, Border.BorderBrushProperty, "RibbonInputBorderBrush");
        Root.ActualThemeChanged += (_, _) => UpdateSelectionVisuals();
        Root.KeyDown += OnKeyDown;
    }

    public event EventHandler<(string Id, bool Visible)>? ToggleChanged;

    public Border Root { get; }

    public ListRow? Selected { get; private set; }

    /// <summary>Accessible name of the list.</summary>
    public string? Name
    {
        get => AutomationProperties.GetName(_panel);
        set => AutomationProperties.SetName(_panel, value ?? string.Empty);
    }

    public void Fill(IEnumerable<ListRow> rows)
    {
        _panel.Children.Clear();
        _items.Clear();
        Selected = null;
        var list = rows.ToList();
        var position = 0;
        foreach (var row in list)
        {
            var content = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, Margin = new Thickness(row.Level * 18, 0, 0, 0) };
            if (row.IsChecked is { } isChecked)
            {
                var check = new CheckBox { IsChecked = isChecked, MinWidth = 0, Padding = new Thickness(0) };
                AutomationProperties.SetName(check, row.Label);
                var id = row.Id;
                check.Checked += (_, _) => ToggleChanged?.Invoke(this, (id, true));
                check.Unchecked += (_, _) => ToggleChanged?.Invoke(this, (id, false));
                content.Children.Add(check);
            }

            if (row.Icon is not null)
            {
                var icon = new Primitives.RibbonIconPresenter { Icon = row.Icon, IconSize = 16 };
                RibbonTheme.SetThemeBrush(icon, Primitives.RibbonIconPresenter.ForegroundProperty, "RibbonIconBrush");
                content.Children.Add(icon);
            }

            content.Children.Add(new TextBlock { Text = row.Label, VerticalAlignment = VerticalAlignment.Center, FontWeight = row.Level == 0 && row.IsChecked is not null ? Microsoft.UI.Text.FontWeights.SemiBold : Microsoft.UI.Text.FontWeights.Normal });
            if (!string.IsNullOrEmpty(row.Detail))
            {
                content.Children.Add(new TextBlock { Text = row.Detail, Opacity = 0.6, FontSize = 11, VerticalAlignment = VerticalAlignment.Center });
            }

            var button = new Button { Content = content, HorizontalAlignment = HorizontalAlignment.Stretch, HorizontalContentAlignment = HorizontalAlignment.Left, Padding = new Thickness(6, 3, 6, 3) };
            if (Application.Current.Resources.TryGetValue("RibbonMenuItemButtonStyle", out var style))
            {
                button.Style = (Style)style;
            }

            AutomationProperties.SetName(button, row.Label);
            AutomationProperties.SetPositionInSet(button, ++position);
            AutomationProperties.SetSizeOfSet(button, list.Count);
            AutomationProperties.SetLevel(button, row.Level + 1);
            if (!string.IsNullOrEmpty(row.Detail))
            {
                AutomationProperties.SetHelpText(button, row.Detail);
            }

            var captured = row;
            button.Click += (_, _) => Select(captured.Id);
            _items.Add((row, button));
            _panel.Children.Add(button);
        }
    }

    public void Select(string id)
    {
        Selected = _items.FirstOrDefault(i => i.Row.Id == id).Row;
        UpdateSelectionVisuals();
    }

    private void UpdateSelectionVisuals()
    {
        var brush = RibbonTheme.GetBrush(Root, "RibbonAccentSubtleBrush");
        foreach (var (row, button) in _items)
        {
            button.Background = ReferenceEquals(row, Selected) && brush is not null ? brush : new SolidColorBrush(Microsoft.UI.Colors.Transparent);
        }
    }

    private void OnKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (_items.Count == 0 || e.OriginalSource is CheckBox)
        {
            return;
        }

        var index = Selected is null ? -1 : _items.FindIndex(i => ReferenceEquals(i.Row, Selected));
        var target = e.Key switch
        {
            Windows.System.VirtualKey.Down => Math.Min(_items.Count - 1, index + 1),
            Windows.System.VirtualKey.Up => Math.Max(0, index - 1),
            Windows.System.VirtualKey.Home => 0,
            Windows.System.VirtualKey.End => _items.Count - 1,
            _ => -2,
        };
        if (target == -2)
        {
            return;
        }

        e.Handled = true;
        if (target < 0 || target == index)
        {
            return;
        }

        var (row, button) = _items[target];
        Select(row.Id);
        button.Focus(FocusState.Keyboard);
        button.StartBringIntoView();
        try
        {
            FrameworkElementAutomationPeer.FromElement(button)?.RaiseAutomationEvent(AutomationEvents.SelectionItemPatternOnElementSelected);
        }
        catch (Exception ex) when (ex is NotImplementedException or NotSupportedException or InvalidOperationException)
        {
            // Automation events are not available on every platform.
        }
    }
}
