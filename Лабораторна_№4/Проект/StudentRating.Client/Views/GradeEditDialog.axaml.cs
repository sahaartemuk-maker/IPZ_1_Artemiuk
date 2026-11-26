using System;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using StudentRating.Client.Models;
using StudentRating.Client.Services;

namespace StudentRating.Client.Views
{
    /// <summary>Діалог редагування оцінок (працює з копією, щоб «Скасувати» не змінювало дані).</summary>
    public partial class GradeEditDialog : Window
    {
        private readonly Student _student;
        private readonly ObservableCollection<Grade> _copy;

        public GradeEditDialog() { InitializeComponent();
            ScreenshotHelper.Attach(this); }

        public GradeEditDialog(Student student) : this()
        {
            _student = student;
            TitleText.Text = student.FullName;
            _copy = new ObservableCollection<Grade>(
                student.Grades.Select(g => new Grade { Subject = g.Subject, Score = g.Score }));
            GradesGrid.ItemsSource = _copy;
            GradesGrid.CellEditEnded += (s, e) => StatusText.Text = string.Empty;
        }

        public System.Collections.Generic.IList<Grade> EditableGrades { get { return _copy; } }

        private async void OnSaveClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!GradesGrid.CommitEdit())
                    throw new InvalidOperationException("Виправте некоректні оцінки (дозволено 0..100).");

                await ServerApi.SaveGradesAsync(_student.Id, _copy);   // запис у БД на сервері
                for (int i = 0; i < _copy.Count; i++) _student.Grades[i].Score = _copy[i].Score;
                _student.RefreshAverage();
                Close(true);
            }
            catch (InvalidOperationException ex)
            {
                StatusText.Text = ex.Message;
            }
            catch (ServerException ex)
            {
                StatusText.Text = "Сервер відхилив: " + ex.Message;
            }
            catch (Exception ex)
            {
                StatusText.Text = "Не вдалося зберегти: " + ex.Message;
            }
        }

        private void OnCancelClick(object sender, RoutedEventArgs e) { Close(false); }
    }
}
