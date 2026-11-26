using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using StudentRating.Client.Models;
using StudentRating.Client.Services;

namespace StudentRating.Client.Views
{
    public class CourseOption
    {
        public string Name { get; set; }
        public int? Value { get; set; }
        public override string ToString() { return Name; }
    }

    public partial class MainWindow : Window
    {
        private readonly MainViewModel _vm = new MainViewModel();
        private ObservableCollection<string> Groups { get { return _vm.Groups; } }
        private ObservableCollection<Student> Rating { get { return _vm.Rating; } }
        private ObservableCollection<Student> BestStudents { get { return _vm.BestStudents; } }
        private SearchCriteria Criteria { get { return _vm.Criteria; } }
        private string SelectedGroup { get { return _vm.SelectedGroup; } set { _vm.SelectedGroup = value; } }
        private Student SelectedStudent { get { return _vm.SelectedStudent; } set { _vm.SelectedStudent = value; } }

        public MainWindow()
        {
            InitializeComponent();
            ScreenshotHelper.Attach(this);

            CourseCombo.ItemsSource = new List<CourseOption>
            {
                new CourseOption { Name = "Усі курси", Value = null },
                new CourseOption { Name = "1 курс", Value = 1 }, new CourseOption { Name = "2 курс", Value = 2 },
                new CourseOption { Name = "3 курс", Value = 3 }, new CourseOption { Name = "4 курс", Value = 4 }
            };
            CourseCombo.SelectedIndex = 0;
            GroupCombo.ItemsSource = new[] { "" }.Concat(Groups).ToList();
            SubjectCombo.ItemsSource = new[] { "" }.Concat(DataService.Subjects).ToList();

            DataContext = _vm;
            if (Groups.Count > 0) SelectedGroup = Groups[0];
        }

        private void OnCourseChanged(object sender, SelectionChangedEventArgs e)
        {
            var opt = CourseCombo.SelectedItem as CourseOption;
            if (Criteria != null) Criteria.Course = opt == null ? null : opt.Value;
        }

        private async void OnShowRatingClick(object sender, RoutedEventArgs e)
        {
            await LoadRating();
        }

        private async Task LoadRating()
        {
            try
            {
                Rating.Clear();
                foreach (var s in DataService.GetRating(SelectedGroup)) Rating.Add(s);
                StatusText.Text = "Завантажено студентів: " + Rating.Count;
            }
            catch (ArgumentException ex)
            {
                await MessageWindow.Show(this, ex.Message, "Увага");
            }
            catch (Exception ex)
            {
                await MessageWindow.Show(this, "Не вдалося отримати рейтинг: " + ex.Message, "Помилка");
            }
        }

        private async void OnSearchClick(object sender, RoutedEventArgs e)
        {
            try
            {
                BestStudents.Clear();
                foreach (var s in DataService.FindBest(Criteria)) BestStudents.Add(s);
                StatusText.Text = BestStudents.Count == 0 ? "Нікого не знайдено" : "Знайдено: " + BestStudents.Count;
            }
            catch (Exception ex)
            {
                await MessageWindow.Show(this, "Помилка пошуку: " + ex.Message, "Помилка");
            }
        }

        private void OnResetClick(object sender, RoutedEventArgs e)
        {
            CourseCombo.SelectedIndex = 0;
            Criteria.Group = ""; Criteria.Subject = "";
            Criteria.MinScore = 60; Criteria.TopCount = 5;
            BestStudents.Clear();
        }

        private async void OnDetailsClick(object sender, RoutedEventArgs e) { await ShowDetails(SelectedStudent); }
        private async void OnRatingDoubleTapped(object sender, TappedEventArgs e) { await ShowDetails(SelectedStudent); }
        private async void OnBestDoubleTapped(object sender, TappedEventArgs e) { await ShowDetails(BestGrid.SelectedItem as Student); }

        private async Task ShowDetails(Student s)
        {
            if (s == null)
            {
                await MessageWindow.Show(this, "Оберіть студента у таблиці.", "Увага");
                return;
            }
            await new StudentDetailsWindow(s).ShowDialog(this);
        }

        private async void OnEditGradesClick(object sender, RoutedEventArgs e)
        {
            var student = SelectedStudent;
            if (student == null)
            {
                await MessageWindow.Show(this, "Спочатку оберіть студента на вкладці «Рейтинг групи».", "Увага");
                return;
            }
            var saved = await new GradeEditDialog(student).ShowDialog<bool>(this);
            if (saved)
            {
                await LoadRating();   // перерахувати місця
                SelectedStudent = student;
                StatusText.Text = "Оцінки збережено: " + student.FullName;
            }
        }

        private async void OnExportClick(object sender, RoutedEventArgs e)
        {
            if (Rating.Count == 0)
            {
                await MessageWindow.Show(this, "Спочатку завантажте рейтинг.", "Увага");
                return;
            }
            try
            {
                var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
                {
                    Title = "Експорт рейтингу",
                    SuggestedFileName = "rating_" + SelectedGroup + ".csv",
                    FileTypeChoices = new[] { new FilePickerFileType("CSV") { Patterns = new[] { "*.csv" } } }
                });
                if (file == null) return;

                var sb = new StringBuilder("Місце;ПІБ;Група;Середній бал\n");
                foreach (var s in Rating)
                    sb.AppendLine(string.Join(";", s.Place, s.FullName, s.Group, s.AverageScore.ToString("F2")));

                using (var stream = await file.OpenWriteAsync())
                using (var writer = new StreamWriter(stream, new UTF8Encoding(true)))
                    await writer.WriteAsync(sb.ToString());
                StatusText.Text = "Експортовано: " + file.Name;
            }
            catch (IOException ex)
            {
                await MessageWindow.Show(this, "Не вдалося записати файл: " + ex.Message, "Помилка");
            }
            catch (UnauthorizedAccessException ex)
            {
                await MessageWindow.Show(this, "Немає доступу до файлу: " + ex.Message, "Помилка");
            }
        }

        private async void OnAboutClick(object sender, RoutedEventArgs e)
        {
            await new AboutWindow().ShowDialog(this);
        }

        private async void OnLogoutClick(object sender, RoutedEventArgs e)
        {
            if (!await MessageWindow.Ask(this, "Вийти з облікового запису?")) return;
            App.CurrentUser = null;
            var login = new LoginWindow();
            App.Desktop.MainWindow = login;
            login.Show();
            Close();
        }
    }

    /// <summary>Дані головного вікна для Data binding.</summary>
    public class MainViewModel : ObservableObject
    {
        private string _selectedGroup;
        private Student _selectedStudent;

        public MainViewModel()
        {
            Groups = new ObservableCollection<string>(DataService.GetGroups());
            Rating = new ObservableCollection<Student>();
            BestStudents = new ObservableCollection<Student>();
            Criteria = new SearchCriteria();
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
    }
}
