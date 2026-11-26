using System.Windows;
using StudentRating.Client.Models;

namespace StudentRating.Client.Views
{
    /// <summary>Діалогове вікно з детальною інформацією про студента.</summary>
    public partial class StudentDetailsWindow : Window
    {
        public StudentDetailsWindow(Student student)
        {
            InitializeComponent();
            DataContext = student;
        }
    }
}
