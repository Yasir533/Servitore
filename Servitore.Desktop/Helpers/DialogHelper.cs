using System.Windows;
using System.Linq;
using Servitore.Desktop.Views.Dialogs;

namespace Servitore.Desktop.Helpers;

public static class DialogHelper
{
    public static void ShowError(string message, string title = "Error")
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            var win = new ModernMessageWindow(message, title, ModernMessageIcon.Error, ModernMessageButton.Ok)
            {
                Owner = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive) ?? Application.Current.MainWindow
            };
            win.ShowDialog();
        });
    }

    public static void ShowInfo(string message, string title = "Servitore")
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            var win = new ModernMessageWindow(message, title, ModernMessageIcon.Info, ModernMessageButton.Ok)
            {
                Owner = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive) ?? Application.Current.MainWindow
            };
            win.ShowDialog();
        });
    }

    public static bool Confirm(string message, string title = "Confirm")
    {
        return Application.Current.Dispatcher.Invoke(() =>
        {
            var win = new ModernMessageWindow(message, title, ModernMessageIcon.Question, ModernMessageButton.YesNo)
            {
                Owner = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive) ?? Application.Current.MainWindow
            };
            win.ShowDialog();
            return win.Result == ModernMessageResult.Yes;
        });
    }

    public static ModernMessageResult ConfirmSaveDiscardCancel(string message, string title = "Unsaved Changes")
    {
        return Application.Current.Dispatcher.Invoke(() =>
        {
            var win = new ModernMessageWindow(message, title, ModernMessageIcon.Question, ModernMessageButton.SaveDiscardCancel)
            {
                Owner = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive) ?? Application.Current.MainWindow
            };
            win.ShowDialog();
            return win.Result;
        });
    }
}
