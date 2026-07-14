using System.Windows;

namespace Servitore.Desktop.Views.Dialogs
{
    public partial class SearchDialog : Window
    {
        public string SearchText { get; private set; } = string.Empty;
        public string SearchBy { get; private set; } = "SCR No.";

        public SearchDialog()
        {
            InitializeComponent();
            SearchTextBox.Focus();
        }

        private void Search_Click(object sender, RoutedEventArgs e)
        {
            SearchText = SearchTextBox.Text.Trim();
            if (SearchByCombo.SelectedItem is System.Windows.Controls.ComboBoxItem item)
            {
                SearchBy = item.Content.ToString() ?? "SCR No.";
            }
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
