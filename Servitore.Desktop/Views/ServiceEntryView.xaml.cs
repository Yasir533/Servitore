using System.Windows.Controls;
using Servitore.Desktop.ViewModels;

namespace Servitore.Desktop.Views;

public partial class ServiceEntryView : UserControl
{
    public ServiceEntryView()
    {
        InitializeComponent();
        var vm = new ServiceEntryViewModel(App.ApiService, App.SignalRService);
        DataContext = vm;
        Loaded += async (s, e) =>
        {
            try
            {
                await vm.LoadCommand.ExecuteAsync(null);
            }
            catch (System.Exception ex)
            {
                Helpers.ClientLogger.Log("Failed to execute LoadCommand in ServiceEntryView", ex);
            }
        };
    }
}
