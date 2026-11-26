using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using StudentRating.Client.Models;
using StudentRating.Client.Services;

namespace StudentRating.Client.Views
{
    /// <summary>Діалог редагування оцінок (працює з копією, щоб «Скасувати» не змінювало дані).</summary>
    public partial class GradeEditDialog : Window
    {
        private readonly Student _student;
        private readonly ObservableCollection<Grade> _copy;

        public GradeEditDialog(Student student)
        {
            InitializeComponent();
            _student = student;
            TitleText.Text = student.FullName;
            _copy = new ObservableCollection<Grade>(
                student.Grades.Select(g => new Grade { Subject = g.Subject, Score = g.Score }));
            GradesGrid.ItemsSource = _copy;
        }

        private void OnSaveClick(object sender, RoutedEventArgs e)
        {
            try
            {
                GradesGrid.CommitEdit(DataGridEditingUnit.Row, true);
                if (System.Windows.Controls.Validation.GetHasError(GradesGrid) ||
                    GradesGrid.ItemContainerGenerator.Items.Cast<object>()
                        .Select(i => GradesGrid.ItemContainerGenerator.ContainerFromItem(i) as DataGridRow)
                        .Any(r => r != null && System.Windows.Controls.Validation.GetHasError(r)))
                    throw new InvalidOperationException("Виправте некоректні оцінки (дозволено 0..100).");

                for (int i = 0; i < _copy.Count; i++) _student.Grades[i].Score = _copy[i].Score;
                DataService.SaveGrades(_student);
                DialogResult = true;
            }
            catch (InvalidOperationException ex)
            {
                StatusText.Text = ex.Message;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не вдалося зберегти: " + ex.Message, "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
