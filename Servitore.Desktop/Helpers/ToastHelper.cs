using MaterialDesignThemes.Wpf;

namespace Servitore.Desktop.Helpers;

public static class ToastHelper
{
    public static ISnackbarMessageQueue? MessageQueue { get; set; }

    public static void ShowToast(string message)
    {
        MessageQueue?.Enqueue(message);
    }
}
