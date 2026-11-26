using Avalonia.Controls;
using Avalonia.Interactivity;

namespace StudentRating.Client.Views
{
    public partial class AboutWindow : Window
    {
        public AboutWindow() { InitializeComponent();
            ScreenshotHelper.Attach(this); }
        private void OnOk(object sender, RoutedEventArgs e) { Close(); }
    }
}
