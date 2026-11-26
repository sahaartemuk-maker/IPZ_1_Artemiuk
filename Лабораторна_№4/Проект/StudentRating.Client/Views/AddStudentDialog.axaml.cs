using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using StudentRating.Client.Services;

namespace StudentRating.Client.Views
{
    /// <summary>Діалог додавання студента (команда ADD_STUDENT).</summary>
    public partial class AddStudentDialog : Window
    {
        public AddStudentDialog() { InitializeComponent(); ScreenshotHelper.Attach(this); }

        public AddStudentDialog(IEnumerable<string> groups) : this()
        {
            GroupBox.ItemsSource = groups.ToList();
            GroupBox.SelectedIndex = 0;
        }

        public void Fill(string last, string first, int groupIndex, int course)
        {
            LastNameBox.Text = last; FirstNameBox.Text = first;
            GroupBox.SelectedIndex = groupIndex; CourseBox.Value = course;
        }

        private async void OnSaveClick(object sender, RoutedEventArgs e) { await SaveAsync(); }

        public async Task SaveAsync()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(LastNameBox.Text) || string.IsNullOrWhiteSpace(FirstNameBox.Text))
                    throw new ArgumentException("Заповніть прізвище та ім'я.");
                SaveButton.IsEnabled = false;
                int id = await ServerApi.AddStudentAsync(LastNameBox.Text.Trim(), FirstNameBox.Text.Trim(),
                    GroupBox.SelectedItem as string, (int)(CourseBox.Value ?? 1));
                Close((int?)id);
            }
            catch (ArgumentException ex) { StatusText.Text = ex.Message; }
            catch (ServerException ex) { StatusText.Text = "Сервер відхилив: " + ex.Message; }
            catch (Exception ex) { StatusText.Text = "Помилка: " + ex.Message; }
            finally { SaveButton.IsEnabled = true; }
        }

        private void OnCancelClick(object sender, RoutedEventArgs e) { Close((int?)null); }
    }
}
