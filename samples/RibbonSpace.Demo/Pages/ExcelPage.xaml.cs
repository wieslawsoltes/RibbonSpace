using System.ComponentModel;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using RibbonSpace.Controls;
using RibbonSpace.Demo.ViewModels;
using Windows.UI.Text;

namespace RibbonSpace.Demo.Pages;

/// <summary>Excel-style page: the ribbon is generated from <see cref="ExcelViewModel.Ribbon"/>.</summary>
public sealed partial class ExcelPage : UserControl
{
    private readonly Dictionary<(int Row, int Column), TextBlock> _cells = [];
    private (int Row, int Column) _selected = (1, 1);

    public ExcelPage()
    {
        ViewModel = new ExcelViewModel();
        InitializeComponent();
        TitleBar.Ribbon = Ribbon;
        BuildSheet();
        foreach (var model in new INotifyPropertyChanged[] { ViewModel.Bold, ViewModel.Italic, ViewModel.Underline, ViewModel.FillColor, ViewModel.FontColor, ViewModel.FontFamily, ViewModel.FontSize, ViewModel.Align[0], ViewModel.Align[1], ViewModel.Align[2] })
        {
            model.PropertyChanged += (_, _) => ApplyToSelection();
        }

        ViewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ExcelViewModel.Status))
            {
                DemoSettings.Write("Excel · " + ViewModel.Status);
            }
        };
        Ribbon.ItemInvoked += (_, e) => ViewModel.Status = e.ItemId ?? e.CommandId ?? ViewModel.Status;
        DemoSettings.Changed += (_, _) => ApplySettings();
        ApplySettings();
    }

    public ExcelViewModel ViewModel { get; }

    private void ApplySettings()
    {
        ViewModel.Ribbon.Density = DemoSettings.Density;
        if (DemoSettings.DisplayMode is { } mode)
        {
            ViewModel.Ribbon.DisplayMode = mode;
        }
    }

    private void BuildSheet()
    {
        const int rows = 24;
        const int columns = 12;
        var header = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 0xF3, 0xF3, 0xF3));
        var line = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 0xE1, 0xE1, 0xE1));
        Sheet.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) });
        for (var c = 1; c <= columns; c++)
        {
            Sheet.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(96) });
        }

        for (var r = 0; r <= rows; r++)
        {
            Sheet.RowDefinitions.Add(new RowDefinition { Height = new GridLength(24) });
        }

        var values = new Dictionary<(int, int), string>
        {
            [(1, 1)] = "Category", [(1, 2)] = "Q1", [(1, 3)] = "Q2", [(1, 4)] = "Q3", [(1, 5)] = "Q4",
            [(2, 1)] = "Rent", [(2, 2)] = "1,200", [(2, 3)] = "1,200", [(2, 4)] = "1,250", [(2, 5)] = "1,250",
            [(3, 1)] = "Software", [(3, 2)] = "860", [(3, 3)] = "910", [(3, 4)] = "940", [(3, 5)] = "990",
            [(4, 1)] = "Travel", [(4, 2)] = "420", [(4, 3)] = "380", [(4, 4)] = "610", [(4, 5)] = "300",
            [(5, 1)] = "Marketing", [(5, 2)] = "1,900", [(5, 3)] = "2,400", [(5, 4)] = "2,100", [(5, 5)] = "2,650",
            [(6, 1)] = "Total", [(6, 2)] = "4,380", [(6, 3)] = "4,890", [(6, 4)] = "4,900", [(6, 5)] = "5,190",
        };
        for (var r = 0; r <= rows; r++)
        {
            for (var c = 0; c <= columns; c++)
            {
                var isHeader = r == 0 || c == 0;
                var text = r == 0 && c > 0 ? ((char)('A' + c - 1)).ToString() : c == 0 && r > 0 ? r.ToString(System.Globalization.CultureInfo.InvariantCulture) : values.GetValueOrDefault((r, c), string.Empty);
                var tb = new TextBlock { Text = text, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(6, 0, 6, 0), FontSize = 13, Foreground = new SolidColorBrush(Microsoft.UI.Colors.Black), HorizontalAlignment = isHeader ? HorizontalAlignment.Center : HorizontalAlignment.Left };
                var border = new Border { Child = tb, BorderBrush = line, BorderThickness = new Thickness(0, 0, 1, 1), Background = isHeader ? header : new SolidColorBrush(Microsoft.UI.Colors.White) };
                Grid.SetRow(border, r);
                Grid.SetColumn(border, c);
                Sheet.Children.Add(border);
                if (!isHeader)
                {
                    var key = (r, c);
                    _cells[key] = tb;
                    border.Tapped += (_, _) => Select(key);
                }
            }
        }

        Select((1, 1));
    }

    private void Select((int Row, int Column) key)
    {
        if (_cells.TryGetValue(_selected, out var previous) && previous.Parent is Border old)
        {
            old.BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 0xE1, 0xE1, 0xE1));
            old.BorderThickness = new Thickness(0, 0, 1, 1);
        }

        _selected = key;
        if (_cells.TryGetValue(key, out var cell) && cell.Parent is Border border)
        {
            border.BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 0x10, 0x7C, 0x41));
            border.BorderThickness = new Thickness(2);
            ViewModel.SelectedCell = $"{(char)('A' + key.Column - 1)}{key.Row}";
            ViewModel.Bold.IsChecked = cell.FontWeight.Weight >= 600;
            ViewModel.Italic.IsChecked = cell.FontStyle == FontStyle.Italic;
        }
    }

    private void ApplyToSelection()
    {
        if (!_cells.TryGetValue(_selected, out var cell) || cell.Parent is not Border border)
        {
            return;
        }

        cell.FontWeight = ViewModel.Bold.IsChecked ? FontWeights.Bold : FontWeights.Normal;
        cell.FontStyle = ViewModel.Italic.IsChecked ? FontStyle.Italic : FontStyle.Normal;
        cell.TextDecorations = ViewModel.Underline.IsChecked ? TextDecorations.Underline : TextDecorations.None;
        if (ViewModel.FontColor.SelectedColor is { } fg)
        {
            cell.Foreground = new SolidColorBrush(RibbonTheme.ToColor(fg));
        }

        border.Background = ViewModel.FillColor.SelectedColor is { } bg ? new SolidColorBrush(RibbonTheme.ToColor(bg)) : new SolidColorBrush(Microsoft.UI.Colors.White);
        if (!string.IsNullOrWhiteSpace(ViewModel.FontFamily.Text))
        {
            cell.FontFamily = new FontFamily(ViewModel.FontFamily.Text);
        }

        if (double.TryParse(ViewModel.FontSize.Text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var size) && size is > 4 and < 100)
        {
            cell.FontSize = size;
        }

        cell.HorizontalAlignment = ViewModel.Align[1].IsChecked ? HorizontalAlignment.Center : ViewModel.Align[2].IsChecked ? HorizontalAlignment.Right : HorizontalAlignment.Left;
    }
}
