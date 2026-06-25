using System.Windows.Controls;
using Servitore.Desktop.ViewModels;

namespace Servitore.Desktop.Views;

public partial class AssetView : UserControl
{
    public AssetView()
    {
        InitializeComponent();
        var vm = new AssetViewModel(App.ApiService, App.BarcodeService);
        DataContext = vm;
        Loaded += async (s, e) =>
        {
            try
            {
                await vm.LoadCommand.ExecuteAsync(null);
            }
            catch (System.Exception ex)
            {
                Helpers.ClientLogger.Log("Failed to execute LoadCommand in AssetView", ex);
            }
        };
    }

    private void DataGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is DataGrid dg && dg.SelectedItem is AssetViewModel.AssetRow row)
        {
            if (DataContext is AssetViewModel vm)
            {
                vm.ViewProfileCommand.Execute(row);
            }
        }
    }
}
