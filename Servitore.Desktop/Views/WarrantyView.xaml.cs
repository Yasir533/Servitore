using System.Windows.Controls;
using Servitore.Desktop.ViewModels;

namespace Servitore.Desktop.Views;

public partial class WarrantyView : UserControl
{
    public WarrantyView()
    {
        InitializeComponent();
        var vm = new WarrantyViewModel(App.ApiService);
        DataContext = vm;
        _ = vm.LoadCommand.ExecuteAsync(null);
    }
}
