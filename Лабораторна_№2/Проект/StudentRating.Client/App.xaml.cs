using System.Windows;
using System.Windows.Threading;

namespace StudentRating.Client
{
    public partial class App : Application
    {
        /// <summary>Поточний авторизований користувач.</summary>
        public static Models.User CurrentUser { get; set; }

        private void OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show("Непередбачена помилка: " + e.Exception.Message, "Помилка",
                MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        }
    }
}
