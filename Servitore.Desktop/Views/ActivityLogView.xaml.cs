using System.Windows.Controls;
using Servitore.Desktop.ViewModels;

namespace Servitore.Desktop.Views;

public partial class ActivityLogView : UserControl
{
    public ActivityLogView()
    {
        InitializeComponent();
        var vm = new ActivityLogViewModel(App.ApiService);
        DataContext = vm;
        _ = vm.LoadCommand.ExecuteAsync(null);
    }
}
