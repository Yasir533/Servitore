using System.Windows;
using System.Windows.Controls;
using Servitore.Desktop.ViewModels;

namespace Servitore.Desktop.Views;

public partial class RecentlyDeletedView : UserControl
{
    public RecentlyDeletedView()
    {
        InitializeComponent();
    }

    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        DataContext = new RecentlyDeletedViewModel();
    }

    private void UserControl_Unloaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is RecentlyDeletedViewModel vm)
        {
            vm.Dispose();
            DataContext = null;
        }
    }
}
