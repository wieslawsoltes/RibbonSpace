using System.Diagnostics;
using System.Reflection;
using System.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace RibbonSpace.Uno.RuntimeTests;

/// <summary>
/// Minimal in-app UI test runner: every public async method marked [RibbonTest] runs on the UI thread inside a real
/// Uno window. Results are printed and written as JUnit XML (RIBBONSPACE_TEST_RESULTS); the exit code is the number
/// of failures. Filter with RIBBONSPACE_TEST_FILTER=substring.
/// </summary>
public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    public static Window? MainWindow { get; private set; }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        MainWindow = new Window { Title = "RibbonSpace Runtime Tests" };
        var host = new Grid();
        MainWindow.Content = host;
        MainWindow.Activate();
        _ = RunAsync(host);
    }

    private static async Task RunAsync(Grid host)
    {
        await Task.Delay(800);
        if (MainWindow?.Content?.XamlRoot is { } root)
        {
            MainWindow.AppWindow.Resize(new Windows.Graphics.SizeInt32 { Width = (int)(1500 * root.RasterizationScale), Height = (int)(900 * root.RasterizationScale) });
        }

        await Task.Delay(400);
        var filter = Environment.GetEnvironmentVariable("RIBBONSPACE_TEST_FILTER");
        var tests = typeof(App).Assembly.GetTypes()
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance).Select(m => (Type: t, Method: m)))
            .Where(x => x.Method.GetCustomAttribute<RibbonTestAttribute>() is not null)
            .Where(x => string.IsNullOrEmpty(filter) || $"{x.Type.Name}.{x.Method.Name}".Contains(filter, StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x.Type.Name).ThenBy(x => x.Method.Name)
            .ToList();
        var results = new List<(string Name, double Seconds, string? Error)>();
        foreach (var (type, method) in tests)
        {
            var name = $"{type.Name}.{method.Name}";
            var watch = Stopwatch.StartNew();
            string? error = null;
            try
            {
                host.Children.Clear();
                var fixture = (RuntimeTestBase)Activator.CreateInstance(type)!;
                fixture.Host = host;
                if (method.Invoke(fixture, null) is Task task)
                {
                    var completed = await Task.WhenAny(task, Task.Delay(20000));
                    if (completed != task)
                    {
                        throw new TimeoutException("Test timed out after 20 s.");
                    }

                    await task;
                }
            }
            catch (Exception ex)
            {
                error = (ex is TargetInvocationException tie ? tie.InnerException ?? ex : ex).ToString();
            }

            results.Add((name, watch.Elapsed.TotalSeconds, error));
            Console.WriteLine($"{(error is null ? "PASS" : "FAIL")}  {name}  ({watch.ElapsedMilliseconds} ms)");
            if (error is not null)
            {
                Console.WriteLine("      " + error.Split('\n')[0]);
            }
        }

        var failures = results.Count(r => r.Error is not null);
        Console.WriteLine($"\n{results.Count - failures} passed, {failures} failed, {results.Count} total");
        if (Environment.GetEnvironmentVariable("RIBBONSPACE_TEST_RESULTS") is { Length: > 0 } path)
        {
            var xml = new StringBuilder();
            xml.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            xml.AppendLine($"<testsuite name=\"RibbonSpace.Uno.RuntimeTests\" tests=\"{results.Count}\" failures=\"{failures}\">");
            foreach (var (name, seconds, error) in results)
            {
                xml.Append($"  <testcase classname=\"{name[..name.IndexOf('.')]}\" name=\"{name[(name.IndexOf('.') + 1)..]}\" time=\"{seconds:0.###}\"");
                xml.AppendLine(error is null ? " />" : $"><failure message=\"{System.Security.SecurityElement.Escape(error.Split('\n')[0])}\">{System.Security.SecurityElement.Escape(error)}</failure></testcase>");
            }

            xml.AppendLine("</testsuite>");
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
            await File.WriteAllTextAsync(path, xml.ToString());
        }

        Environment.Exit(failures);
    }
}

/// <summary>Marks a runtime UI test.</summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class RibbonTestAttribute : Attribute
{
}

/// <summary>Base of runtime test fixtures.</summary>
public abstract class RuntimeTestBase
{
    public Grid Host { get; set; } = null!;

    /// <summary>Adds an element to the window and waits for layout.</summary>
    protected async Task<T> Mount<T>(T element, double width = double.NaN)
        where T : FrameworkElement
    {
        var container = new Border { Width = width, HorizontalAlignment = double.IsNaN(width) ? HorizontalAlignment.Stretch : HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top, Child = element };
        Host.Children.Add(container);
        await Settle();
        return element;
    }

    /// <summary>Resizes the container of a mounted element.</summary>
    protected async Task Resize(FrameworkElement element, double width)
    {
        if (element.Parent is Border border)
        {
            border.Width = width;
        }

        await Settle();
    }

    /// <summary>Waits for layout / dispatcher work.</summary>
    protected static async Task Settle(int milliseconds = 250)
    {
        await Task.Delay(milliseconds);
        MainWindow?.Content?.UpdateLayout();
        await Task.Delay(50);
    }

    private static Window? MainWindow => App.MainWindow;
}

/// <summary>Assertions.</summary>
public static class Assert
{
    public static void True(bool condition, string message = "Expected true")
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    public static void False(bool condition, string message = "Expected false") => True(!condition, message);

    public static void Equal<T>(T expected, T actual, string? message = null)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException($"{message ?? "Values differ"}: expected <{expected}>, actual <{actual}>");
        }
    }

    public static T NotNull<T>(T? value, string message = "Expected a value")
        where T : class
        => value ?? throw new InvalidOperationException(message);
}
