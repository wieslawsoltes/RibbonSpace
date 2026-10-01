using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using RibbonSpace.Controls;

namespace RibbonSpace.Demo.Pages;

/// <summary>Non-ribbon command UIs built with RibbonSpace: menu bar, tool palette, options bar, rails, status bar.</summary>
public sealed partial class ToolsPage : UserControl
{
    public ToolsPage()
    {
        InitializeComponent();
        foreach (var tool in Tools.Items.OfType<RibbonToggleButton>())
        {
            tool.Checked += (s, _) =>
            {
                var button = (RibbonToggleButton)s!;
                Options.ActiveContext = button.Tag as string;
                ToolStatus.Text = button.Label;
                CanvasText.Text = $"{button.Label} is active";
            };
        }

        Tools.ItemInvoked += (_, e) => DemoSettings.Write($"Tools · {(e.Item as IRibbonItem)?.Label}");
    }
}
