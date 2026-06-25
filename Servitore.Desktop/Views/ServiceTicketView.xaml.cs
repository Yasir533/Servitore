using System.Windows.Controls;
using Servitore.Desktop.ViewModels;

namespace Servitore.Desktop.Views;

public partial class ServiceTicketView : UserControl
{
    public ServiceTicketView()
    {
        InitializeComponent();
        var vm = new ServiceTicketViewModel(App.ApiService, App.SignalRService);
        DataContext = vm;
        _ = vm.LoadCommand.ExecuteAsync(null);
    }
}
