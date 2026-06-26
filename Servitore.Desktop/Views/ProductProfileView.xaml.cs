using System.Windows;
using System.Windows.Controls;
using Servitore.Desktop.ViewModels;

namespace Servitore.Desktop.Views;

public partial class ProductProfileView : UserControl
{
    private readonly ProductProfileViewModel _viewModel;

    public ProductProfileView(int productId)
    {
        InitializeComponent();
        _viewModel = new ProductProfileViewModel(App.ApiService, productId);
        DataContext = _viewModel;
    }

    private async void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            await _viewModel.LoadCommand.ExecuteAsync(null);
        }
        catch (System.Exception ex)
        {
            Helpers.ClientLogger.Log("Failed to execute LoadCommand in ProductProfileView", ex);
        }
    }
}
