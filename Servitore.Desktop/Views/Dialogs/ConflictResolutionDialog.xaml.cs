using System;
using System.Collections.Generic;
using System.Windows;

namespace Servitore.Desktop.Views.Dialogs;

public enum ConflictResolutionResult
{
    Cancel,
    KeepMine,
    Reload
}

public class ConflictDiff
{
    public string FieldName { get; set; } = string.Empty;
    public string LocalValue { get; set; } = string.Empty;
    public string ServerValue { get; set; } = string.Empty;
}

public partial class ConflictResolutionDialog : Window
{
    public ConflictResolutionResult Result { get; private set; } = ConflictResolutionResult.Cancel;

    public ConflictResolutionDialog(List<ConflictDiff> differences)
    {
        InitializeComponent();
        DiffsGrid.ItemsSource = differences;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        Result = ConflictResolutionResult.Cancel;
        DialogResult = false;
        Close();
    }

    private void KeepMine_Click(object sender, RoutedEventArgs e)
    {
        Result = ConflictResolutionResult.KeepMine;
        DialogResult = true;
        Close();
    }

    private void Reload_Click(object sender, RoutedEventArgs e)
    {
        Result = ConflictResolutionResult.Reload;
        DialogResult = true;
        Close();
    }
}
