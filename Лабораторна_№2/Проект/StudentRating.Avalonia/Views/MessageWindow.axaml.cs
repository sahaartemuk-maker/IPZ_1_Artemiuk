using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace StudentRating.Client.Views
{
    /// <summary>Простий діалог повідомлень (аналог MessageBox з WPF).</summary>
    public partial class MessageWindow : Window
    {
        public MessageWindow() { InitializeComponent();
            ScreenshotHelper.Attach(this); }

        private MessageWindow(string title, string text, bool question) : this()
        {
            Title = title;
            MessageText.Text = text;
            if (!question)
            {
                YesButton.Content = "OK";
                NoButton.IsVisible = false;
            }
        }

        public static Task Show(Window owner, string text, string title = "Повідомлення")
        {
            return new MessageWindow(title, text, false).ShowDialog(owner);
        }

        public static Task<bool> Ask(Window owner, string text, string title = "Підтвердження")
        {
            return new MessageWindow(title, text, true).ShowDialog<bool>(owner);
        }

        private void OnYes(object sender, RoutedEventArgs e) { Close(true); }
        private void OnNo(object sender, RoutedEventArgs e) { Close(false); }
    }
}
