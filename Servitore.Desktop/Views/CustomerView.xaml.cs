using System.Windows.Controls;
using Servitore.Desktop.ViewModels;

namespace Servitore.Desktop.Views;

public partial class CustomerView : UserControl
{
    public CustomerView()
    {
        InitializeComponent();
        var vm = new CustomerViewModel(App.ApiService);
        DataContext = vm;
        _ = vm.LoadCommand.ExecuteAsync(null);
    }
}
