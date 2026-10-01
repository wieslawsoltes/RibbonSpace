using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace RibbonSpace.Demo;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    public static Window? MainWindow { get; private set; }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        MainWindow = new Window { Title = "RibbonSpace Gallery" };
        var shell = new ShellPage();
        MainWindow.Content = shell;
        MainWindow.Activate();
        _ = Capture.TryRunAsync(MainWindow, shell);
    }
}
