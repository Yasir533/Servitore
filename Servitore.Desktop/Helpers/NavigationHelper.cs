using System.Windows.Controls;

namespace Servitore.Desktop.Helpers;

// Centralizes "navigate the main content area to view X" logic so views
// don't need to know about each other directly.
public static class NavigationHelper
{
    private static ContentControl? _host;

    public static void Initialize(ContentControl host)
    {
        _host = host;
    }

    public static void NavigateTo(UserControl view)
    {
        if (_host != null)
        {
            _host.Content = view;
        }
    }

    public static void NavigateTo(ContentControl host, UserControl view)
    {
        host.Content = view;
    }
}
