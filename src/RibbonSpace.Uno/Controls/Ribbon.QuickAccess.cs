using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using RibbonSpace.Localization;

namespace RibbonSpace.Controls;

public partial class Ribbon
{
    /// <summary>Identifies <see cref="QuickAccessToolBar"/>.</summary>
    public static readonly DependencyProperty QuickAccessToolBarProperty = DependencyProperty.Register(nameof(QuickAccessToolBar), typeof(RibbonQuickAccessToolBar), typeof(Ribbon), new PropertyMetadata(null, (d, e) => ((Ribbon)d).OnQuickAccessToolBarChanged((RibbonQuickAccessToolBar?)e.OldValue, (RibbonQuickAccessToolBar?)e.NewValue)));

    /// <summary>Identifies <see cref="QuickAccessPosition"/>.</summary>
    public static readonly DependencyProperty QuickAccessPositionProperty = DependencyProperty.Register(nameof(QuickAccessPosition), typeof(RibbonQuickAccessPosition), typeof(Ribbon), new PropertyMetadata(RibbonQuickAccessPosition.AboveRibbon, (d, _) => ((Ribbon)d).OnQuickAccessOptionsChanged()));

    /// <summary>Identifies <see cref="IsQuickAccessVisible"/>.</summary>
    public static readonly DependencyProperty IsQuickAccessVisibleProperty = DependencyProperty.Register(nameof(IsQuickAccessVisible), typeof(bool), typeof(Ribbon), new PropertyMetadata(true, (d, _) => ((Ribbon)d).OnQuickAccessOptionsChanged()));

    /// <summary>Identifies <see cref="ShowQuickAccessLabels"/>.</summary>
    public static readonly DependencyProperty ShowQuickAccessLabelsProperty = DependencyProperty.Register(nameof(ShowQuickAccessLabels), typeof(bool), typeof(Ribbon), new PropertyMetadata(false, (d, _) => ((Ribbon)d).OnQuickAccessOptionsChanged()));

    /// <summary>Identifies <see cref="IsQuickAccessHostedExternally"/>.</summary>
    public static readonly DependencyProperty IsQuickAccessHostedExternallyProperty = DependencyProperty.Register(nameof(IsQuickAccessHostedExternally), typeof(bool), typeof(Ribbon), new PropertyMetadata(false, (d, _) => ((Ribbon)d).UpdateQuickAccessPlacement()));

    private Border? _quickAccessAboveHost;
    private Border? _quickAccessBelowHost;

    /// <summary>Raised when the QAT position, visibility or items change (hosts such as <see cref="RibbonTitleBar"/> relayout).</summary>
    public event EventHandler? QuickAccessChanged;

    /// <summary>The Quick Access Toolbar.</summary>
    public RibbonQuickAccessToolBar? QuickAccessToolBar { get => (RibbonQuickAccessToolBar?)GetValue(QuickAccessToolBarProperty); set => SetValue(QuickAccessToolBarProperty, value); }

    /// <summary>QAT position.</summary>
    public RibbonQuickAccessPosition QuickAccessPosition { get => (RibbonQuickAccessPosition)GetValue(QuickAccessPositionProperty); set => SetValue(QuickAccessPositionProperty, value); }

    /// <summary>QAT visibility.</summary>
    public bool IsQuickAccessVisible { get => (bool)GetValue(IsQuickAccessVisibleProperty); set => SetValue(IsQuickAccessVisibleProperty, value); }

    /// <summary>QAT labels.</summary>
    public bool ShowQuickAccessLabels { get => (bool)GetValue(ShowQuickAccessLabelsProperty); set => SetValue(ShowQuickAccessLabelsProperty, value); }

    /// <summary>True when a <see cref="RibbonTitleBar"/> hosts the QAT above the ribbon.</summary>
    public bool IsQuickAccessHostedExternally { get => (bool)GetValue(IsQuickAccessHostedExternallyProperty); set => SetValue(IsQuickAccessHostedExternallyProperty, value); }

    /// <summary>Ids of commands offered in the QAT customize drop-down (Office "Customize Quick Access Toolbar").</summary>
    public ObservableCollection<string> QuickAccessCandidateIds { get; } = [];

    private void InitializeQuickAccess() => QuickAccessToolBar = new RibbonQuickAccessToolBar();

    private void OnQuickAccessToolBarChanged(RibbonQuickAccessToolBar? oldValue, RibbonQuickAccessToolBar? newValue)
    {
        if (oldValue is not null)
        {
            oldValue.Ribbon = null;
        }

        if (newValue is not null)
        {
            newValue.Ribbon = this;
            foreach (var item in newValue.Items)
            {
                RibbonItemHelper.SetOwner(item, this);
            }
        }

        UpdateQuickAccessPlacement();
        ApplyQuickAccessLayout();
        if (oldValue is not null || newValue?.Items.Count > 0)
        {
            OnQuickAccessItemsChanged();
        }
    }

    /// <summary>
    /// QAT item ids as declared by the application (captured when the ribbon first loads, before user state is
    /// applied). Used by "Reset" in the customize dialog and <see cref="ResetQuickAccess"/>.
    /// </summary>
    public IReadOnlyList<string>? DefaultQuickAccessItemIds { get; private set; }

    /// <summary>Default QAT position (captured on first load).</summary>
    public RibbonQuickAccessPosition DefaultQuickAccessPosition { get; private set; }

    internal void CaptureQuickAccessDefaults()
    {
        if (DefaultQuickAccessItemIds is null)
        {
            DefaultQuickAccessItemIds = GetQuickAccessItemIds();
            DefaultQuickAccessPosition = QuickAccessPosition;
        }
    }

    /// <summary>Restores the application's default QAT items and position.</summary>
    public void ResetQuickAccess()
    {
        CaptureQuickAccessDefaults();
        var state = GetState();
        state.QuickAccessItemIds = DefaultQuickAccessItemIds!.ToList();
        state.QuickAccessPosition = DefaultQuickAccessPosition;
        state.IsQuickAccessVisible = true;
        ApplyState(state);
    }

    private void OnQuickAccessOptionsChanged()
    {
        UpdateQuickAccessPlacement();
        ApplyQuickAccessLayout();
        RaiseStateChanged();
        OnModelQuickAccessChanged();
        QuickAccessChanged?.Invoke(this, EventArgs.Empty);
    }

    internal void OnQuickAccessItemsChanged()
    {
        SyncQuickAccessToModel();
        InvalidateShortcuts();
        RaiseStateChanged();
        QuickAccessChanged?.Invoke(this, EventArgs.Empty);
    }

    private void ApplyQuickAccessLayout()
    {
        if (QuickAccessToolBar is { } qat)
        {
            qat.Metrics = Metrics;
            qat.ShowLabels = ShowQuickAccessLabels;
            qat.ApplyLayouts();
        }
    }

    internal void UpdateQuickAccessPlacement()
    {
        var qat = QuickAccessToolBar;
        if (qat is null || _quickAccessAboveHost is null || _quickAccessBelowHost is null)
        {
            return;
        }

        var above = QuickAccessPosition == RibbonQuickAccessPosition.AboveRibbon;
        var hiddenByFullScreen = VisibilityMode == RibbonVisibilityMode.FullScreen && !IsFullScreenRevealed;
        Border? target = !IsQuickAccessVisible || hiddenByFullScreen ? null : above ? (IsQuickAccessHostedExternally ? null : _quickAccessAboveHost) : _quickAccessBelowHost;
        foreach (var host in new[] { _quickAccessAboveHost, _quickAccessBelowHost })
        {
            if (!ReferenceEquals(host, target) && ReferenceEquals(host.Child, qat))
            {
                host.Child = null;
            }

            host.Visibility = ReferenceEquals(host, target) ? Visibility.Visible : Visibility.Collapsed;
        }

        if (target is not null && !ReferenceEquals(target.Child, qat))
        {
            if (VisualTreeHelper.GetParent(qat) is Border other)
            {
                other.Child = null;
            }
            else if (VisualTreeHelper.GetParent(qat) is ContentPresenter presenter)
            {
                presenter.Content = null;
            }

            target.Child = qat;
        }

        ApplyQuickAccessLayout();
    }

    /// <summary>Returns the QAT copy of an item, if any.</summary>
    public FrameworkElement? FindQuickAccessCopy(FrameworkElement item)
        => QuickAccessToolBar?.Items.OfType<FrameworkElement>().FirstOrDefault(i => ReferenceEquals(i, item) || ReferenceEquals(RibbonItemHelper.GetSourceItem(i), item));

    /// <summary>True when an item (or its copy) is in the QAT.</summary>
    public bool IsInQuickAccess(FrameworkElement item) => FindQuickAccessCopy(RibbonItemHelper.GetSourceItem(item) ?? item) is not null || QuickAccessToolBar?.Items.Contains(item) == true;

    /// <summary>Adds a linked copy of an item to the QAT. Returns false when not possible.</summary>
    public bool AddToQuickAccess(FrameworkElement item)
    {
        var source = RibbonItemHelper.GetSourceItem(item) ?? item;
        if (QuickAccessToolBar is null || IsInQuickAccess(source) || source is not IRibbonItem { CanAddToQuickAccess: true } ribbonItem)
        {
            return false;
        }

        var copy = CreateQuickAccessCopy(source);
        if (copy is null)
        {
            return false;
        }

        QuickAccessToolBar.Items.Add(copy);
        return true;
    }

    private FrameworkElement? CreateQuickAccessCopy(FrameworkElement source)
    {
        if (source is not IRibbonItem { CanAddToQuickAccess: true } ribbonItem || ribbonItem.CreateLinkedCopy() is not { } copy)
        {
            return null;
        }

        RibbonItemHelper.SetSourceItem(copy, source);
        RibbonItemHelper.SetOwner(copy, this);
        return copy;
    }

    /// <summary>Adds an item by id.</summary>
    public bool AddToQuickAccess(string id) => FindItem(id) is { } item && AddToQuickAccess(item);

    /// <summary>Removes an item (or its linked copy) from the QAT.</summary>
    public bool RemoveFromQuickAccess(FrameworkElement item)
    {
        if (QuickAccessToolBar is null)
        {
            return false;
        }

        var target = QuickAccessToolBar.Items.Contains(item) ? item : FindQuickAccessCopy(RibbonItemHelper.GetSourceItem(item) ?? item);
        return target is not null && QuickAccessToolBar.Items.Remove(target);
    }

    /// <summary>Ids of items in the QAT.</summary>
    public IReadOnlyList<string> GetQuickAccessItemIds()
        => QuickAccessToolBar?.Items.OfType<FrameworkElement>()
            .Select(i => ((RibbonItemHelper.GetSourceItem(i) ?? i) as IRibbonItem)?.Id)
            .OfType<string>()
            .ToArray() ?? [];

    /// <summary>Builds the QAT customize drop-down.</summary>
    public MenuFlyout BuildQuickAccessCustomizeMenu()
    {
        var strings = RibbonStrings.Current;
        var menu = new MenuFlyout();
        menu.Items.Add(new MenuFlyoutItem { Text = strings.CustomizeQuickAccessToolbar, IsEnabled = false });
        var candidates = QuickAccessCandidateIds.Select(id => FindItem(id)).OfType<FrameworkElement>()
            .Concat(QuickAccessToolBar?.Items.OfType<FrameworkElement>().Select(i => RibbonItemHelper.GetSourceItem(i) ?? i) ?? [])
            .Distinct()
            .ToArray();
        foreach (var candidate in candidates)
        {
            if (candidate is not IRibbonItem ribbonItem)
            {
                continue;
            }

            var toggle = new ToggleMenuFlyoutItem { Text = ribbonItem.Label ?? ribbonItem.Id ?? string.Empty, IsChecked = IsInQuickAccess(candidate) };
            toggle.Click += (_, _) =>
            {
                if (toggle.IsChecked)
                {
                    AddToQuickAccess(candidate);
                }
                else
                {
                    RemoveFromQuickAccess(candidate);
                }
            };
            menu.Items.Add(toggle);
        }

        if (candidates.Length > 0)
        {
            menu.Items.Add(new MenuFlyoutSeparator());
        }

        menu.Items.Add(RibbonMenu.Item(strings.MoreCommands, () => ShowCustomizeDialog(RibbonCustomizePage.QuickAccessToolbar)));
        menu.Items.Add(RibbonMenu.Item(QuickAccessPosition == RibbonQuickAccessPosition.AboveRibbon ? strings.ShowBelowRibbon : strings.ShowAboveRibbon, ToggleQuickAccessPosition));
        var labels = new ToggleMenuFlyoutItem { Text = strings.ShowCommandLabels, IsChecked = ShowQuickAccessLabels };
        labels.Click += (_, _) => ShowQuickAccessLabels = labels.IsChecked;
        menu.Items.Add(labels);
        menu.Items.Add(RibbonMenu.Item(strings.HideQuickAccessToolbar, () => IsQuickAccessVisible = false));
        return menu;
    }

    /// <summary>Moves the QAT above / below the ribbon.</summary>
    public void ToggleQuickAccessPosition()
        => QuickAccessPosition = QuickAccessPosition == RibbonQuickAccessPosition.AboveRibbon ? RibbonQuickAccessPosition.BelowRibbon : RibbonQuickAccessPosition.AboveRibbon;
}
