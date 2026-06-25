using System.Windows;
using System.Windows.Controls;
using Servitore.Desktop.ViewModels;

namespace Servitore.Desktop.Views;

public partial class AssetProfileView : UserControl
{
    private readonly AssetProfileViewModel _viewModel;

    public AssetProfileView(int assetId)
    {
        InitializeComponent();
        _viewModel = new AssetProfileViewModel(App.ApiService, assetId);
        DataContext = _viewModel;
    }

    private async void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        await _viewModel.LoadCommand.ExecuteAsync(null);
    }
}
