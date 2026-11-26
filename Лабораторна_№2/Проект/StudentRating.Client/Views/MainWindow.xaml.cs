using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using Microsoft.Win32;
using StudentRating.Client.Models;
using StudentRating.Client.Services;

namespace StudentRating.Client.Views
{
    public class CourseOption
    {
        public string Name { get; set; }
        public int? Value { get; set; }
    }

    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private string _selectedGroup;
        private Student _selectedStudent;

        public MainWindow()
        {
            InitializeComponent();
            Groups = new ObservableCollection<string>(DataService.GetGroups());
            Rating = new ObservableCollection<Student>();
            BestStudents = new ObservableCollection<Student>();
            Criteria = new SearchCriteria();

            CourseCombo.ItemsSource = new List<CourseOption>
            {
                new CourseOption { Name = "Усі курси", Value = null },
                new CourseOption { Name = "1 курс", Value = 1 }, new CourseOption { Name = "2 курс", Value = 2 },
                new CourseOption { Name = "3 курс", Value = 3 }, new CourseOption { Name = "4 курс", Value = 4 }
            };
            GroupCombo.ItemsSource = new[] { "" }.Concat(Groups).ToList();
            SubjectCombo.ItemsSource = new[] { "" }.Concat(DataService.Subjects).ToList();

            DataContext = this;
            if (Groups.Count > 0) SelectedGroup = Groups[0];
        }

        public User CurrentUser { get { return App.CurrentUser; } }
        public ObservableCollection<string> Groups { get; private set; }
        public ObservableCollection<Student> Rating { get; private set; }
        public ObservableCollection<Student> BestStudents { get; private set; }
        public SearchCriteria Criteria { get; private set; }

        public string SelectedGroup
        {
            get { return _selectedGroup; }
            set { _selectedGroup = value; OnPropertyChanged(); }
        }

        public Student SelectedStudent
        {
            get { return _selectedStudent; }
            set { _selectedStudent = value; OnPropertyChanged(); }
        }

        private void OnShowRatingClick(object sender, RoutedEventArgs e)
        {
            try
            {
                Rating.Clear();
                foreach (var s in DataService.GetRating(SelectedGroup)) Rating.Add(s);
                StatusText.Text = "Завантажено студентів: " + Rating.Count;
            }
            catch (ArgumentException ex)
            {
                MessageBox.Show(ex.Message, "Увага", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не вдалося отримати рейтинг: " + ex.Message, "Помилка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnSearchClick(object sender, RoutedEventArgs e)
        {
            try
            {
                BestStudents.Clear();
                foreach (var s in DataService.FindBest(Criteria)) BestStudents.Add(s);
                StatusText.Text = BestStudents.Count == 0 ? "Нікого не знайдено" : "Знайдено: " + BestStudents.Count;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Помилка пошуку: " + ex.Message, "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnResetClick(object sender, RoutedEventArgs e)
        {
            Criteria.Course = null; Criteria.Group = ""; Criteria.Subject = "";
            Criteria.MinScore = 60; Criteria.TopCount = 5;
            BestStudents.Clear();
        }

        private void OnDetailsClick(object sender, RoutedEventArgs e) { ShowDetails(SelectedStudent); }

        private void OnBestDetailsClick(object sender, RoutedEventArgs e) { ShowDetails(BestGrid.SelectedItem as Student); }

        private void ShowDetails(Student s)
        {
            if (s == null)
            {
                MessageBox.Show("Оберіть студента у таблиці.", "Увага", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            new StudentDetailsWindow(s) { Owner = this }.ShowDialog();
        }

        private void OnEditGradesClick(object sender, RoutedEventArgs e)
        {
            if (SelectedStudent == null)
            {
                MessageBox.Show("Спочатку оберіть студента на вкладці «Рейтинг групи».", "Увага",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            var student = SelectedStudent;
            var dlg = new GradeEditDialog(student) { Owner = this };
            if (dlg.ShowDialog() == true)
            {
                OnShowRatingClick(null, null);   // перерахувати місця
                SelectedStudent = student;
                StatusText.Text = "Оцінки збережено: " + student.FullName;
            }
        }

        private void OnExportClick(object sender, RoutedEventArgs e)
        {
            if (Rating.Count == 0)
            {
                MessageBox.Show("Спочатку завантажте рейтинг.", "Увага", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            var dlg = new SaveFileDialog { Filter = "CSV (*.csv)|*.csv", FileName = "rating_" + SelectedGroup + ".csv" };
            if (dlg.ShowDialog(this) != true) return;
            try
            {
                var sb = new StringBuilder("Місце;ПІБ;Група;Середній бал\n");
                foreach (var s in Rating)
                    sb.AppendLine(string.Join(";", s.Place, s.FullName, s.Group, s.AverageScore.ToString("F2")));
                File.WriteAllText(dlg.FileName, sb.ToString(), Encoding.UTF8);
                StatusText.Text = "Експортовано у " + dlg.FileName;
            }
            catch (IOException ex)
            {
                MessageBox.Show("Не вдалося записати файл: " + ex.Message, "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (UnauthorizedAccessException ex)
            {
                MessageBox.Show("Немає доступу до файлу: " + ex.Message, "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnAboutClick(object sender, RoutedEventArgs e)
        {
            new AboutWindow { Owner = this }.ShowDialog();
        }

        private void OnLogoutClick(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Вийти з облікового запису?", "Підтвердження",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
            App.CurrentUser = null;
            var login = new LoginWindow();
            Application.Current.MainWindow = login;
            login.Show();
            Close();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
