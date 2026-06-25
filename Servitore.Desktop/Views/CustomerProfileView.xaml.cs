using System.Windows;
using System.Windows.Controls;
using Servitore.Desktop.ViewModels;

namespace Servitore.Desktop.Views;

public partial class CustomerProfileView : UserControl
{
    private readonly CustomerProfileViewModel _viewModel;

    public CustomerProfileView(int customerId)
    {
        InitializeComponent();
        _viewModel = new CustomerProfileViewModel(App.ApiService, customerId);
        DataContext = _viewModel;
    }

    private async void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        await _viewModel.LoadCommand.ExecuteAsync(null);
    }

    private void AssetsGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is DataGrid dg && dg.SelectedItem is Servitore.Shared.Models.CustomerAssetDto asset)
        {
            if (DataContext is CustomerProfileViewModel vm)
            {
                vm.ViewAssetDetailsCommand.Execute(asset);
            }
        }
    }
}
