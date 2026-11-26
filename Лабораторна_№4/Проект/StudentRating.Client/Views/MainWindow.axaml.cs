using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Sockets;
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
            DataContext = _vm;
        }

        public MainViewModel ViewModel { get { return _vm; } }

        /// <summary>Завантажити довідники (групи, предмети) з сервера.</summary>
        public async Task InitializeDataAsync()
        {
            await Guard(async () =>
            {
                var groups = await ServerApi.GetGroupsAsync();
                _vm.Groups.Clear();
                foreach (var g in groups) _vm.Groups.Add(g);
                GroupCombo.ItemsSource = new[] { "" }.Concat(groups).ToList();
                SubjectCombo.ItemsSource = new[] { "" }.Concat(ServerApi.Subjects).ToList();
                if (_vm.Groups.Count > 0) _vm.SelectedGroup = _vm.Groups[0];
                StatusText.Text = $"Підключено до {ServerApi.Host}:{ServerApi.Port}";
            });
        }

        public void SelectTab(int index) { Tabs.SelectedIndex = index; }

        /// <summary>Обгортка: перехоплення помилок зв'язку та сервера.</summary>
        private async Task Guard(Func<Task> action)
        {
            try
            {
                await action();
            }
            catch (ServerException ex)
            {
                await MessageWindow.Show(this, ex.Message, ex.Status == "ACCESS_DENIED" ? "Доступ заборонено" : "Помилка сервера");
            }
            catch (ArgumentException ex)
            {
                await MessageWindow.Show(this, ex.Message, "Увага");
            }
            catch (Exception ex) when (ex is IOException || ex is SocketException || ex is TimeoutException)
            {
                ServerApi.Disconnect();
                StatusText.Text = "Немає з'єднання з сервером";
                await MessageWindow.Show(this, "Втрачено з'єднання з сервером: " + ex.Message, "Помилка зв'язку");
            }
        }

        private void OnCourseChanged(object sender, SelectionChangedEventArgs e)
        {
            var opt = CourseCombo.SelectedItem as CourseOption;
            _vm.Criteria.Course = opt == null ? null : opt.Value;
        }

        private async void OnShowRatingClick(object sender, RoutedEventArgs e) { await LoadRatingAsync(); }

        public async Task LoadRatingAsync()
        {
            await Guard(async () =>
            {
                var list = await ServerApi.GetRatingAsync(_vm.SelectedGroup);
                _vm.Rating.Clear();
                foreach (var s in list) _vm.Rating.Add(s);
                StatusText.Text = $"Група {_vm.SelectedGroup}: {list.Count} студентів (дані з сервера)";
            });
        }

        private async void OnSearchClick(object sender, RoutedEventArgs e) { await SearchAsync(); }

        public async Task SearchAsync()
        {
            await Guard(async () =>
            {
                var list = await ServerApi.FindBestAsync(_vm.Criteria);
                _vm.BestStudents.Clear();
                foreach (var s in list) _vm.BestStudents.Add(s);
                StatusText.Text = list.Count == 0 ? "Нікого не знайдено" : "Знайдено: " + list.Count;
            });
        }

        private void OnResetClick(object sender, RoutedEventArgs e)
        {
            CourseCombo.SelectedIndex = 0;
            _vm.Criteria.Group = ""; _vm.Criteria.Subject = "";
            _vm.Criteria.MinScore = 60; _vm.Criteria.TopCount = 5;
            _vm.BestStudents.Clear();
        }

        private async void OnDetailsClick(object sender, RoutedEventArgs e) { await ShowDetails(_vm.SelectedStudent); }
        private async void OnRatingDoubleTapped(object sender, TappedEventArgs e) { await ShowDetails(_vm.SelectedStudent); }
        private async void OnBestDoubleTapped(object sender, TappedEventArgs e) { await ShowDetails(BestGrid.SelectedItem as Student); }

        private async Task ShowDetails(Student s)
        {
            if (s == null)
            {
                await MessageWindow.Show(this, "Оберіть студента у таблиці.", "Увага");
                return;
            }
            await Guard(async () =>
            {
                var fresh = await ServerApi.GetStudentAsync(s.Id);   // актуальні дані з БД
                fresh.Place = s.Place;
                await new StudentDetailsWindow(fresh).ShowDialog(this);
            });
        }

        private async void OnEditGradesClick(object sender, RoutedEventArgs e)
        {
            var student = _vm.SelectedStudent;
            if (student == null)
            {
                await MessageWindow.Show(this, "Спочатку оберіть студента на вкладці «Рейтинг групи».", "Увага");
                return;
            }
            var saved = await new GradeEditDialog(student).ShowDialog<bool>(this);
            if (saved)
            {
                await LoadRatingAsync();
                _vm.SelectedStudent = _vm.Rating.FirstOrDefault(x => x.Id == student.Id);
                StatusText.Text = "Оцінки збережено в БД: " + student.FullName;
            }
        }

        private async void OnAddStudentClick(object sender, RoutedEventArgs e)
        {
            var dlg = new AddStudentDialog(_vm.Groups);
            var id = await dlg.ShowDialog<int?>(this);
            if (id.HasValue)
            {
                await InitializeDataAsync();
                StatusText.Text = $"Студента додано (Id={id.Value})";
            }
        }

        private async void OnExportClick(object sender, RoutedEventArgs e)
        {
            if (_vm.Rating.Count == 0)
            {
                await MessageWindow.Show(this, "Спочатку завантажте рейтинг.", "Увага");
                return;
            }
            try
            {
                var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
                {
                    Title = "Експорт рейтингу",
                    SuggestedFileName = "rating_" + _vm.SelectedGroup + ".csv",
                    FileTypeChoices = new[] { new FilePickerFileType("CSV") { Patterns = new[] { "*.csv" } } }
                });
                if (file == null) return;
                var sb = new StringBuilder("Місце;ПІБ;Група;Середній бал\n");
                foreach (var s in _vm.Rating)
                    sb.AppendLine(string.Join(";", s.Place, s.FullName, s.Group, s.AverageScore.ToString("F2")));
                using (var stream = await file.OpenWriteAsync())
                using (var writer = new StreamWriter(stream, new UTF8Encoding(true)))
                    await writer.WriteAsync(sb.ToString());
                StatusText.Text = "Експортовано: " + file.Name;
            }
            catch (IOException ex) { await MessageWindow.Show(this, "Не вдалося записати файл: " + ex.Message, "Помилка"); }
            catch (UnauthorizedAccessException ex) { await MessageWindow.Show(this, "Немає доступу до файлу: " + ex.Message, "Помилка"); }
        }

        private async void OnAboutClick(object sender, RoutedEventArgs e)
        {
            await new AboutWindow().ShowDialog(this);
        }

        private async void OnLogoutClick(object sender, RoutedEventArgs e)
        {
            if (!await MessageWindow.Ask(this, "Вийти з облікового запису?")) return;
            ServerApi.Disconnect();
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
            Groups = new ObservableCollection<string>();
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
