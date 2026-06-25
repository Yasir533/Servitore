using System;
using System.IO;

namespace Servitore.Desktop.Helpers;

public static class ClientLogger
{
    private static readonly string LogFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "client.log");
    private static readonly object LockObj = new();

    public static void Log(string message, Exception? ex = null)
    {
        try
        {
            lock (LockObj)
            {
                using var writer = new StreamWriter(LogFilePath, true);
                writer.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}");
                if (ex != null)
                {
                    writer.WriteLine(ex.ToString());
                }
                writer.WriteLine(new string('-', 80));
            }
        }
        catch
        {
            // Fail-safe: ignore logging failures to prevent crashes
        }
    }
}
