using System.Windows.Controls;
using Servitore.Desktop.ViewModels;

namespace Servitore.Desktop.Views;

public partial class ProductView : UserControl
{
    public ProductView()
    {
        InitializeComponent();
        var vm = new ProductViewModel(App.ApiService, App.BarcodeService);
        DataContext = vm;
        Loaded += async (s, e) =>
        {
            try
            {
                await vm.LoadCommand.ExecuteAsync(null);
            }
            catch (System.Exception ex)
            {
                Helpers.ClientLogger.Log("Failed to execute LoadCommand in ProductView", ex);
            }
        };
        Unloaded += (s, e) =>
        {
            vm.Dispose();
        };
    }

    private void DataGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is DataGrid dg && dg.SelectedItem is ProductViewModel.ProductRow row)
        {
            if (DataContext is ProductViewModel vm)
            {
                vm.ViewProfileCommand.Execute(row);
            }
        }
    }
}
