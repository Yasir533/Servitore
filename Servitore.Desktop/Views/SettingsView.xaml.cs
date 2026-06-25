using System.Windows.Controls;
using Servitore.Desktop.ViewModels;

namespace Servitore.Desktop.Views;

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
        var vm = new SettingsViewModel(App.ApiService);
        DataContext = vm;
        _ = vm.LoadCommand.ExecuteAsync(null);
    }
}
