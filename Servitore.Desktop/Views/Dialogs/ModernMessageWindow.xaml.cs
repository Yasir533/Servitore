using System;
using System.Windows;
using System.Windows.Media;
using MaterialDesignThemes.Wpf;

namespace Servitore.Desktop.Views.Dialogs;

public enum ModernMessageResult
{
    None,
    Ok,
    Cancel,
    Yes,
    No,
    Save,
    Discard
}

public enum ModernMessageIcon
{
    Info,
    Warning,
    Error,
    Question
}

public enum ModernMessageButton
{
    Ok,
    OkCancel,
    YesNo,
    SaveDiscardCancel
}

public partial class ModernMessageWindow : Window
{
    public ModernMessageResult Result { get; private set; } = ModernMessageResult.None;

    public ModernMessageWindow(string message, string title, ModernMessageIcon icon, ModernMessageButton buttons)
    {
        InitializeComponent();

        MessageText.Text = message;
        TitleText.Text = title;

        ConfigureIcon(icon);
        ConfigureButtons(buttons);

        Loaded += (s, e) =>
        {
            if (BtnOk.Visibility == Visibility.Visible) BtnOk.Focus();
            else if (BtnYes.Visibility == Visibility.Visible) BtnYes.Focus();
            else if (BtnSave.Visibility == Visibility.Visible) BtnSave.Focus();
        };
    }

    private void ConfigureIcon(ModernMessageIcon icon)
    {
        switch (icon)
        {
            case ModernMessageIcon.Info:
                MsgIcon.Kind = PackIconKind.Information;
                MsgIcon.Foreground = new SolidColorBrush(Color.FromRgb(33, 150, 243)); // Blue
                break;
            case ModernMessageIcon.Warning:
                MsgIcon.Kind = PackIconKind.AlertCircle;
                MsgIcon.Foreground = new SolidColorBrush(Color.FromRgb(255, 179, 0)); // Amber
                break;
            case ModernMessageIcon.Error:
                MsgIcon.Kind = PackIconKind.AlertOctagon;
                MsgIcon.Foreground = new SolidColorBrush(Color.FromRgb(229, 57, 53)); // Red
                break;
            case ModernMessageIcon.Question:
                MsgIcon.Kind = PackIconKind.HelpCircle;
                MsgIcon.Foreground = new SolidColorBrush(Color.FromRgb(63, 81, 181)); // Indigo
                break;
        }
    }

    private void ConfigureButtons(ModernMessageButton buttons)
    {
        // Hide all first
        BtnOk.Visibility = Visibility.Collapsed;
        BtnCancel.Visibility = Visibility.Collapsed;
        BtnYes.Visibility = Visibility.Collapsed;
        BtnNo.Visibility = Visibility.Collapsed;
        BtnSave.Visibility = Visibility.Collapsed;
        BtnDiscard.Visibility = Visibility.Collapsed;

        switch (buttons)
        {
            case ModernMessageButton.Ok:
                BtnOk.Visibility = Visibility.Visible;
                break;
            case ModernMessageButton.OkCancel:
                BtnOk.Visibility = Visibility.Visible;
                BtnCancel.Visibility = Visibility.Visible;
                break;
            case ModernMessageButton.YesNo:
                BtnYes.Visibility = Visibility.Visible;
                BtnNo.Visibility = Visibility.Visible;
                break;
            case ModernMessageButton.SaveDiscardCancel:
                BtnSave.Visibility = Visibility.Visible;
                BtnDiscard.Visibility = Visibility.Visible;
                BtnCancel.Visibility = Visibility.Visible;
                break;
        }
    }

    private void BtnOk_Click(object sender, RoutedEventArgs e)
    {
        Result = ModernMessageResult.Ok;
        DialogResult = true;
        Close();
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        Result = ModernMessageResult.Cancel;
        DialogResult = false;
        Close();
    }

    private void BtnYes_Click(object sender, RoutedEventArgs e)
    {
        Result = ModernMessageResult.Yes;
        DialogResult = true;
        Close();
    }

    private void BtnNo_Click(object sender, RoutedEventArgs e)
    {
        Result = ModernMessageResult.No;
        DialogResult = false;
        Close();
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        Result = ModernMessageResult.Save;
        DialogResult = true;
        Close();
    }

    private void BtnDiscard_Click(object sender, RoutedEventArgs e)
    {
        Result = ModernMessageResult.Discard;
        DialogResult = false; // We can set DialogResult to false for discard to indicate we shouldn't save, but handled through Result
        Close();
    }
}
