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
        Loaded += async (s, e) =>
        {
            try
            {
                await vm.LoadCommand.ExecuteAsync(null);
            }
            catch (System.Exception ex)
            {
                Helpers.ClientLogger.Log("Failed to execute LoadCommand in ServiceTicketView", ex);
            }
        };
    }
}
