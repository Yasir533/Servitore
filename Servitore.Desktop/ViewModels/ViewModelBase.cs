using CommunityToolkit.Mvvm.ComponentModel;

namespace Servitore.Desktop.ViewModels;

// Common base for all ViewModels — gives every VM ObservableObject's
// INotifyPropertyChanged support via CommunityToolkit.Mvvm.
public abstract class ViewModelBase : ObservableObject
{
}
