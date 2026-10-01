using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using RibbonSpace.Controls;

namespace RibbonSpace.Demo.Pages;

/// <summary>PowerPoint-style page defaulting to the simplified ribbon; theme gallery with live preview.</summary>
public sealed partial class PowerPointPage : UserControl
{
    private RibbonGalleryItem? _theme;

    public PowerPointPage()
    {
        InitializeComponent();
        TitleBar.Ribbon = Ribbon;
        for (var i = 1; i <= 4; i++)
        {
            Thumbnails.Children.Add(new Border
            {
                Height = 90,
                CornerRadius = new CornerRadius(3),
                Background = new SolidColorBrush(Microsoft.UI.Colors.White),
                BorderThickness = new Thickness(i == 1 ? 2 : 1),
                BorderBrush = i == 1 ? (Brush)Application.Current.Resources["RibbonAccentBrush"] : new SolidColorBrush(Windows.UI.Color.FromArgb(0x22, 0, 0, 0)),
                Child = new TextBlock { Text = $"Slide {i}", Margin = new Thickness(8), FontSize = 11, Foreground = new SolidColorBrush(Microsoft.UI.Colors.Gray) },
            });
        }

        Themes.ItemPreview += (_, e) => ApplyTheme(e.Item as RibbonGalleryItem ?? _theme);
        Themes.ItemClick += (_, e) =>
        {
            _theme = e.Item as RibbonGalleryItem;
            ApplyTheme(_theme);
        };
        Ribbon.ItemInvoked += (_, e) =>
        {
            Status.Text = $"{e.ItemId}{(e.Parameter is null ? string.Empty : $" ({e.Parameter})")}";
            DemoSettings.Write("PowerPoint · " + Status.Text);
        };
        DemoSettings.Changed += (_, _) =>
        {
            Ribbon.Density = DemoSettings.Density;
            if (DemoSettings.DisplayMode is { } mode)
            {
                Ribbon.DisplayMode = mode;
            }
        };
    }

    private void ApplyTheme(RibbonGalleryItem? theme)
    {
        Slide.Background = theme?.PreviewBackground ?? new SolidColorBrush(Microsoft.UI.Colors.White);
        var fg = theme?.PreviewForeground ?? new SolidColorBrush(Windows.UI.Color.FromArgb(255, 0x26, 0x26, 0x26));
        SlideTitle.Foreground = fg;
        SlideSubtitle.Foreground = fg;
    }
}
