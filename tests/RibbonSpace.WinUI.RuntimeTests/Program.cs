using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

namespace RibbonSpace.Uno.RuntimeTests;

/// <summary>
/// Windows App SDK host of the runtime UI tests (the Uno host is in RibbonSpace.Uno.RuntimeTests/Platforms). Same as the generated entry
/// point, plus unhandled exceptions written to the standard error stream.
/// </summary>
internal static class Program
{
    [STAThread]
    private static void Main()
    {
        WinRT.ComWrappersSupport.InitializeComWrappers();
        Application.Start(_ =>
        {
            SynchronizationContext.SetSynchronizationContext(new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread()));
            try
            {
                new App().UnhandledException += (_, e) => Console.Error.WriteLine($"Unhandled exception: {e.Message}{Environment.NewLine}{e.Exception}");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Startup failed: {ex}");
                throw;
            }
        });
    }
}
