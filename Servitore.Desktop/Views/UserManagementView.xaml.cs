using System.Windows.Controls;
using Servitore.Desktop.ViewModels;

namespace Servitore.Desktop.Views;

public partial class UserManagementView : UserControl
{
    public UserManagementView()
    {
        InitializeComponent();
        var vm = new UserManagementViewModel(App.ApiService);
        DataContext = vm;
        _ = vm.LoadCommand.ExecuteAsync(null);
    }
}
