using System.Windows;
using Servitore.Desktop.ViewModels;

namespace Servitore.Desktop.Views.Dialogs;

public partial class CustomerEditDialog : Window
{
    public CustomerViewModel.CustomerRow Customer { get; }

    public CustomerEditDialog(CustomerViewModel.CustomerRow? customer = null)
    {
        InitializeComponent();
        
        if (customer != null)
        {
            Customer = customer;
            TitleText.Text = "Edit Customer Details";
            NameBox.Text = customer.CustomerName;
            ContactBox.Text = customer.ContactPerson;
            MobileBox.Text = customer.Mobile;
            EmailBox.Text = customer.Email;
            AddressBox.Text = customer.Address;
        }
        else
        {
            Customer = new CustomerViewModel.CustomerRow();
            TitleText.Text = "Add New Customer";
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var name = NameBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            MessageBox.Show("Customer Name is required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        Customer.CustomerName = name;
        Customer.ContactPerson = ContactBox.Text.Trim();
        Customer.Mobile = MobileBox.Text.Trim();
        Customer.Email = EmailBox.Text.Trim();
        Customer.Address = AddressBox.Text.Trim();

        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
