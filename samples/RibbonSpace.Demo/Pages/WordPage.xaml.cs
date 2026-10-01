using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using RibbonSpace.Commands;
using RibbonSpace.Controls;
using Windows.UI.Text;

namespace RibbonSpace.Demo.Pages;

/// <summary>Word-style page declared entirely in XAML; formatting commands act on the sample document.</summary>
public sealed partial class WordPage : UserControl
{
    private string _style = "Normal";

    public WordPage()
    {
        InitializeComponent();
        DemoSettings.WordRibbon = Ribbon;
        Ribbon.CommandCatalog = new RibbonCommandCatalog();
        Ribbon.CommandCatalog.Register("save", "Save", _ => Log("Document saved"), shortcut: "Ctrl+S", description: "Save the document", category: "File");
        BackstageSave.Command = new RibbonRelayCommand(() => Log("Saved from backstage"));
        Ribbon.QuickAccessCandidateIds.Add("cut");
        Ribbon.QuickAccessCandidateIds.Add("copy");
        Ribbon.QuickAccessCandidateIds.Add("paste");
        Ribbon.QuickAccessCandidateIds.Add("bold");
        Ribbon.QuickAccessCandidateIds.Add("newComment");
        Ribbon.ItemInvoked += OnItemInvoked;
        Ribbon.SelectedTabChanged += (_, e) => Log($"Tab: {e.NewTab?.Header}");
        FontFamily.Committed += (_, e) => DocText.FontFamily = new FontFamily(e.Text ?? "Segoe UI");
        FontSize.Committed += (_, e) =>
        {
            if (double.TryParse(e.Text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var size) && size is > 1 and < 400)
            {
                DocText.FontSize = size;
            }
        };
        FontColor.ColorSelected += (_, color) => DocText.Foreground = new SolidColorBrush(color ?? Microsoft.UI.Colors.Black);
        FontColor.Click += (_, _) => DocText.Foreground = new SolidColorBrush(FontColor.SelectedColor ?? Microsoft.UI.Colors.Black);
        PageColor.ColorSelected += (_, color) => PageSheet.Background = new SolidColorBrush(color ?? Microsoft.UI.Colors.White);
        Styles.ItemPreview += (_, e) => ApplyStyle(e.Value as string ?? _style);
        Styles.ItemClick += (_, e) =>
        {
            _style = e.Value as string ?? "Normal";
            ApplyStyle(_style);
            Log($"Style: {_style}");
        };
        TablePicker.SizePicked += (_, size) =>
        {
            InsertTable.Flyout?.Hide();
            TableSample.Visibility = Visibility.Visible;
            Ribbon.SetActiveContextualGroups("table");
            Log($"Inserted {size.Columns}×{size.Rows} table");
        };
        ToggleTable.Click += (_, _) =>
        {
            var show = TableSample.Visibility == Visibility.Collapsed;
            TableSample.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            Ribbon.SetActiveContextualGroups(show ? ["table"] : []);
        };
        ToggleDark.Click += (_, _) =>
        {
            if (XamlRoot?.Content is FrameworkElement root)
            {
                root.RequestedTheme = root.ActualTheme == ElementTheme.Dark ? ElementTheme.Light : ElementTheme.Dark;
            }
        };
        Zoom.ValueChanged += (_, value) => PageSheet.RenderTransform = new ScaleTransform { ScaleX = value / 100, ScaleY = value / 100, CenterX = 380 };
        DemoSettings.Changed += (_, _) => ApplySettings();
        Loaded += (_, _) => TitleBar.Ribbon = Ribbon;
        ApplySettings();
    }

    private void ApplySettings()
    {
        Ribbon.Density = DemoSettings.Density;
        if (DemoSettings.DisplayMode is { } mode)
        {
            Ribbon.DisplayMode = mode;
        }
    }

    private void OnItemInvoked(object? sender, RibbonItemInvokedEventArgs e)
    {
        var id = e.ItemId ?? e.CommandId ?? "(no id)";
        switch (id)
        {
            case "bold":
            case "italic":
            case "underline":
            case "strikethrough":
                UpdateFont();
                break;
            case "alignLeft":
                DocText.TextAlignment = TextAlignment.Left;
                break;
            case "alignCenter":
                DocText.TextAlignment = TextAlignment.Center;
                break;
            case "alignRight":
                DocText.TextAlignment = TextAlignment.Right;
                break;
            case "justify":
                DocText.TextAlignment = TextAlignment.Justify;
                break;
            case "growFont":
                DocText.FontSize = Math.Min(96, DocText.FontSize + 2);
                FontSize.Text = DocText.FontSize.ToString(System.Globalization.CultureInfo.InvariantCulture);
                break;
            case "shrinkFont":
                DocText.FontSize = Math.Max(8, DocText.FontSize - 2);
                FontSize.Text = DocText.FontSize.ToString(System.Globalization.CultureInfo.InvariantCulture);
                break;
            case "clearFormatting":
                Bold.IsChecked = Italic.IsChecked = Underline.IsChecked = Strike.IsChecked = false;
                DocText.FontSize = 16;
                DocText.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Black);
                UpdateFont();
                break;
        }

        Log($"{id}{(e.Parameter is null ? string.Empty : $" ({e.Parameter})")}");
    }

    private void UpdateFont()
    {
        DocText.FontWeight = Bold.IsChecked == true ? FontWeights.Bold : FontWeights.Normal;
        DocText.FontStyle = Italic.IsChecked == true ? FontStyle.Italic : FontStyle.Normal;
        DocText.TextDecorations = (Underline.IsChecked == true ? TextDecorations.Underline : TextDecorations.None) | (Strike.IsChecked == true ? TextDecorations.Strikethrough : TextDecorations.None);
    }

    private void ApplyStyle(string style)
    {
        (DocTitle.FontSize, DocTitle.Foreground) = style switch
        {
            "Title" => (40d, new SolidColorBrush(Microsoft.UI.Colors.Black)),
            "Heading1" => (30d, new SolidColorBrush(Windows.UI.Color.FromArgb(255, 0x2F, 0x54, 0x96))),
            "Heading2" => (24d, new SolidColorBrush(Windows.UI.Color.FromArgb(255, 0x2F, 0x54, 0x96))),
            "Subtitle" => (22d, new SolidColorBrush(Windows.UI.Color.FromArgb(255, 0x59, 0x59, 0x59))),
            "IntenseQuote" or "Quote" => (26d, new SolidColorBrush(Windows.UI.Color.FromArgb(255, 0x40, 0x40, 0x40))),
            _ => (30d, new SolidColorBrush(Windows.UI.Color.FromArgb(255, 0x1F, 0x38, 0x64))),
        };
        DocTitle.FontStyle = style is "Emphasis" or "Quote" or "IntenseQuote" ? FontStyle.Italic : FontStyle.Normal;
        DocTitle.FontWeight = style is "Strong" ? FontWeights.Bold : FontWeights.Normal;
    }

    private void Log(string message)
    {
        LastCommand.Text = "Last command: " + message;
        DemoSettings.Write("Word · " + message);
    }
}
