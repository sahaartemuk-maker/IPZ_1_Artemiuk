using Avalonia.Controls;
using Avalonia.Interactivity;
using StudentRating.Client.Models;

namespace StudentRating.Client.Views
{
    /// <summary>Діалогове вікно з детальною інформацією про студента.</summary>
    public partial class StudentDetailsWindow : Window
    {
        public StudentDetailsWindow() { InitializeComponent();
            ScreenshotHelper.Attach(this); }

        public StudentDetailsWindow(Student student) : this()
        {
            DataContext = student;
        }

        private void OnClose(object sender, RoutedEventArgs e) { Close(); }
    }
}
