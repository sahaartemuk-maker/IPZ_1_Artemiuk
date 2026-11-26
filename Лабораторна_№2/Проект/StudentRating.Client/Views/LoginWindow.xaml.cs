using System;
using System.Windows;
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
            DataContext = _form;   // Data binding: поле Login прив'язане до User.Login
            LoginBox.Focus();
        }

        private void OnLoginClick(object sender, RoutedEventArgs e)
        {
            try
            {
                StatusText.Text = string.Empty;
                App.CurrentUser = DataService.Login(_form.Login, PasswordBox.Password);
                var main = new MainWindow();
                Application.Current.MainWindow = main;
                main.Show();
                Close();
            }
            catch (UnauthorizedAccessException ex)
            {
                StatusText.Text = ex.Message;
                PasswordBox.Clear();
            }
            catch (ArgumentException ex)
            {
                StatusText.Text = ex.Message;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Помилка входу: " + ex.Message, "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnExitClick(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}
