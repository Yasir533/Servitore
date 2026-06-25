using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using Servitore.Desktop.Services;

namespace Servitore.Desktop;

public partial class App : Application
{
    // Process-wide singletons; swap for a proper DI container (e.g. Microsoft.Extensions.DependencyInjection)
    // as the app grows beyond this scaffold.
    public static ApiService ApiService { get; } = new();
    public static AuthenticationService AuthenticationService { get; } = new(ApiService);
    public static SignalRService SignalRService { get; } = new();
    public static BarcodeService BarcodeService { get; } = new();

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        AppDomain.CurrentDomain.UnhandledException += (s, ev) => 
            LogUnhandledException(ev.ExceptionObject as Exception, "AppDomain");

        DispatcherUnhandledException += (s, ev) => 
        {
            LogUnhandledException(ev.Exception, "Dispatcher");
            ev.Handled = true;
        };

        TaskScheduler.UnobservedTaskException += (s, ev) => 
        {
            LogUnhandledException(ev.Exception, "TaskScheduler");
            ev.SetObserved();
        };
    }

    private void LogUnhandledException(Exception? ex, string source)
    {
        if (ex == null) return;
        try
        {
            var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "client_errors.log");
            var logText = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{source}] ERROR: {ex.Message}{Environment.NewLine}{ex.StackTrace}{Environment.NewLine}{Environment.NewLine}";
            File.AppendAllText(logPath, logText);
            
            MessageBox.Show($"An unexpected error occurred: {ex.Message}. Details written to client_errors.log", "Application Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        catch
        {
            // Ignore write failures
        }
    }
}
