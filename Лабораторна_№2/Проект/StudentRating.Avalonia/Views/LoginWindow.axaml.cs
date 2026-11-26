using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using StudentRating.Client.Models;
using StudentRating.Client.Services;

namespace StudentRating.Client.Views
{
    public partial class LoginWindow : Window
    {
        private readonly User _form = new User();

        public LoginWindow()
        {
            InitializeComponent();
            ScreenshotHelper.Attach(this);
            DataContext = _form;   // Data binding: поле «Логін» прив'язане до User.Login
            Opened += (s, e) => LoginBox.Focus();
        }

        private async void OnLoginClick(object sender, RoutedEventArgs e)
        {
            try
            {
                StatusText.Text = string.Empty;
                App.CurrentUser = DataService.Login(_form.Login, PasswordBox.Text);
                var main = new MainWindow();
                App.Desktop.MainWindow = main;
                main.Show();
                Close();
            }
            catch (UnauthorizedAccessException ex)
            {
                StatusText.Text = ex.Message;
                PasswordBox.Text = string.Empty;
            }
            catch (ArgumentException ex)
            {
                StatusText.Text = ex.Message;
            }
            catch (Exception ex)
            {
                await MessageWindow.Show(this, "Помилка входу: " + ex.Message, "Помилка");
            }
        }

        private void OnExitClick(object sender, RoutedEventArgs e)
        {
            App.Desktop.Shutdown();
        }
    }
}
