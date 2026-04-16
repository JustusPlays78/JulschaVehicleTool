using System.Windows;
using System.Windows.Input;

namespace JulschaVehicleTool.App.Views;

public partial class PasswordDialog : Window
{
    public string? Password { get; private set; }
    private readonly bool _confirm;

    public PasswordDialog(string prompt, bool confirm = false)
    {
        InitializeComponent();
        PromptText.Text = prompt;
        _confirm = confirm;
        ConfirmPanel.Visibility = confirm ? Visibility.Visible : Visibility.Collapsed;
    }

    private void OnOk(object sender, RoutedEventArgs e)
    {
        if (_confirm && PwdBox.Password != ConfirmBox.Password)
        {
            ErrorText.Visibility = Visibility.Visible;
            return;
        }
        if (string.IsNullOrEmpty(PwdBox.Password))
        {
            ErrorText.Text = "Password cannot be empty.";
            ErrorText.Visibility = Visibility.Visible;
            return;
        }
        Password = PwdBox.Password;
        DialogResult = true;
    }

    private void OnCancel(object sender, RoutedEventArgs e) => DialogResult = false;

    private void OnPasswordBoxKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
            OnOk(sender, new RoutedEventArgs());
        else if (e.Key == Key.Escape)
            DialogResult = false;
    }
}
