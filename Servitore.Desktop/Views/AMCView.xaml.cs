using System.Windows.Controls;
using Servitore.Desktop.ViewModels;

namespace Servitore.Desktop.Views;

public partial class AMCView : UserControl
{
    public AMCView()
    {
        InitializeComponent();
        var vm = new AMCViewModel(App.ApiService);
        DataContext = vm;
        _ = vm.LoadCommand.ExecuteAsync(null);
    }
}
