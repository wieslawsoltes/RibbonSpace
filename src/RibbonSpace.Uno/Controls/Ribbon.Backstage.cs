using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace RibbonSpace.Controls;

public partial class Ribbon
{
    /// <summary>Identifies <see cref="Backstage"/>.</summary>
    public static readonly DependencyProperty BackstageProperty = DependencyProperty.Register(nameof(Backstage), typeof(RibbonBackstage), typeof(Ribbon), new PropertyMetadata(null, (d, e) => ((Ribbon)d).OnBackstageChanged((RibbonBackstage?)e.OldValue, (RibbonBackstage?)e.NewValue)));

    /// <summary>Identifies <see cref="IsBackstageOpen"/>.</summary>
    public static readonly DependencyProperty IsBackstageOpenProperty = DependencyProperty.Register(nameof(IsBackstageOpen), typeof(bool), typeof(Ribbon), new PropertyMetadata(false, (d, e) => ((Ribbon)d).OnIsBackstageOpenChanged((bool)e.NewValue)));

    private Popup? _backstagePopup;

    /// <summary>Raised after the backstage opened.</summary>
    public event EventHandler? BackstageOpened;

    /// <summary>Raised after the backstage closed.</summary>
    public event EventHandler? BackstageClosed;

    /// <summary>Backstage shown by the application (File) button.</summary>
    public RibbonBackstage? Backstage { get => (RibbonBackstage?)GetValue(BackstageProperty); set => SetValue(BackstageProperty, value); }

    /// <summary>Opens / closes the backstage.</summary>
    public bool IsBackstageOpen { get => (bool)GetValue(IsBackstageOpenProperty); set => SetValue(IsBackstageOpenProperty, value); }

    private void OnBackstageChanged(RibbonBackstage? oldValue, RibbonBackstage? newValue)
    {
        if (oldValue is not null)
        {
            oldValue.CloseRequested -= OnBackstageCloseRequested;
            oldValue.Ribbon = null;
        }

        if (newValue is not null)
        {
            newValue.CloseRequested += OnBackstageCloseRequested;
            newValue.Ribbon = this;
            RibbonItemHelper.SetOwner(newValue, this);
        }

        if (IsBackstageOpen)
        {
            IsBackstageOpen = false;
        }
    }

    private void OnBackstageCloseRequested(object? sender, EventArgs e) => IsBackstageOpen = false;

    private void OnIsBackstageOpenChanged(bool open)
    {
        if (open)
        {
            OpenBackstageCore();
        }
        else
        {
            CloseBackstageCore();
        }

        // Use the actual value: opening can fail (no backstage) and reset IsBackstageOpen re-entrantly.
        OnModelBackstageChanged(IsBackstageOpen);
    }

    private void OpenBackstageCore()
    {
        var backstage = Backstage;
        if (backstage is null)
        {
            IsBackstageOpen = false;
            return;
        }

        if (XamlRoot is null || !IsLoaded)
        {
            // Opened before the ribbon is in the tree (e.g. from the model in XAML): open once loaded.
            return;
        }

        if (_backstagePopup is { IsOpen: true } && ReferenceEquals(_backstagePopup.Child, backstage))
        {
            return;
        }

        HideKeyTips();
        CloseMinimizedPopup();
        _backstagePopup ??= new Popup();
        _backstagePopup.Child = backstage;
        _backstagePopup.XamlRoot = XamlRoot;
        backstage.RequestedTheme = ActualTheme;
        if (backstage.ReadLocalValue(DataContextProperty) == DependencyProperty.UnsetValue)
        {
            backstage.DataContext = DataContext;
        }

        SizeBackstage();
        XamlRoot.Changed -= OnXamlRootChanged;
        XamlRoot.Changed += OnXamlRootChanged;
        AttachPopupKeyboard(backstage);
        _backstagePopup.HorizontalOffset = 0;
        _backstagePopup.VerticalOffset = 0;
        _backstagePopup.IsOpen = true;
        backstage.EnsureSelection();
        backstage.AnimateContent();
        backstage.Focus(FocusState.Programmatic);
        BackstageOpened?.Invoke(this, EventArgs.Empty);
    }

    private void OnXamlRootChanged(XamlRoot sender, XamlRootChangedEventArgs args) => SizeBackstage();

    private void SizeBackstage()
    {
        if (Backstage is { } backstage && XamlRoot is { } root)
        {
            backstage.Width = root.Size.Width;
            backstage.Height = root.Size.Height;
        }
    }

    private void CloseBackstageCore()
    {
        if (XamlRoot is not null)
        {
            XamlRoot.Changed -= OnXamlRootChanged;
        }

        HideKeyTips();
        if (_backstagePopup is { IsOpen: true })
        {
            _backstagePopup.IsOpen = false;
            _backstagePopup.Child = null;
            BackstageClosed?.Invoke(this, EventArgs.Empty);
            _applicationButton?.Focus(FocusState.Programmatic);
        }
    }
}
