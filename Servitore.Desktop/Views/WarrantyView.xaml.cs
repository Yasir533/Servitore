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
        Loaded += async (s, e) =>
        {
            try
            {
                await vm.LoadCommand.ExecuteAsync(null);
            }
            catch (System.Exception ex)
            {
                Helpers.ClientLogger.Log("Failed to execute LoadCommand in WarrantyView", ex);
            }
        };
    }
}
