using System;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;
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

        public void Fill(string login, string password)
        {
            _form.Login = login;
            PasswordBox.Text = password;
        }

        private async void OnLoginClick(object sender, RoutedEventArgs e) { await LoginAsync(); }

        /// <summary>Повертає головне вікно після успішного входу (або null).</summary>
        public async Task<MainWindow> LoginAsync()
        {
            LoginButton.IsEnabled = false;
            StatusText.Text = "З'єднання з сервером…";
            try
            {
                ServerApi.Host = HostBox.Text?.Trim();
                ServerApi.Port = int.Parse(PortBox.Text);
                await ServerApi.ConnectAsync();
                App.CurrentUser = await ServerApi.LoginAsync(_form.Login, PasswordBox.Text);

                var main = new MainWindow();
                App.Desktop.MainWindow = main;
                main.Show();
                await main.InitializeDataAsync();
                Close();
                return main;
            }
            catch (FormatException) { StatusText.Text = "Порт має бути числом."; }
            catch (ServerException ex) { StatusText.Text = ex.Message; PasswordBox.Text = string.Empty; }
            catch (ArgumentException ex) { StatusText.Text = ex.Message; }
            catch (SocketException) { StatusText.Text = $"Не вдалося підключитися до {ServerApi.Host}:{ServerApi.Port}. Чи запущено сервер?"; }
            catch (TimeoutException ex) { StatusText.Text = ex.Message; }
            catch (IOException ex) { StatusText.Text = "Помилка зв'язку: " + ex.Message; }
            finally { LoginButton.IsEnabled = true; }
            return null;
        }

        private void OnExitClick(object sender, RoutedEventArgs e)
        {
            App.Desktop.Shutdown();
        }
    }
}
