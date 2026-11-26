using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using StudentRating.Client.Models;
using StudentRating.Client.Services;
using StudentRating.Client.Views;

namespace StudentRating.Client
{
    /// <summary>
    /// Демо-режим (--demo): програма сама проходить сценарій роботи з сервером
    /// і зберігає знімок кожного кроку для звіту.
    /// </summary>
    public static class DemoRunner
    {
        public static bool Enabled;

        private static Task Wait(int ms = 900) { return Task.Delay(ms); }

        public static async Task Run(LoginWindow login)
        {
            try
            {
                await Wait(1200);
                // 1. Невірний пароль
                login.Fill("teacher", "0000");
                await login.LoginAsync();
                await Wait();
                ScreenshotHelper.Save(login, "login_wrong_password");

                // 2. Правильний вхід
                login.Fill("teacher", "2222");
                await Wait(400);
                ScreenshotHelper.Save(login, "login");
                var main = await login.LoginAsync();
                if (main == null) throw new InvalidOperationException("Вхід не вдався");
                main.Width = 960; main.Height = 600;
                await Wait(1200);
                var vm = main.ViewModel;

                // 3. Рейтинг групи
                await main.LoadRatingAsync();
                vm.SelectedStudent = vm.Rating.FirstOrDefault(s => s.LastName == "Артемюк") ?? vm.Rating.FirstOrDefault();
                await Wait();
                ScreenshotHelper.Save(main, "rating");

                // 4. Картка студента (дані з БД)
                var st = await ServerApi.GetStudentAsync(vm.SelectedStudent.Id);
                st.Place = vm.SelectedStudent.Place;
                var details = new StudentDetailsWindow(st);
                details.Show(main);
                await Wait();
                ScreenshotHelper.Save(details, "student_details");
                details.Close();

                // 5. Пошук кращих
                main.SelectTab(1);
                vm.Criteria.Subject = "Програмування";
                vm.Criteria.MinScore = 70;
                vm.Criteria.TopCount = 5;
                await main.SearchAsync();
                await Wait();
                ScreenshotHelper.Save(main, "find_best");

                // 6. Вкладка «Оцінки»
                main.SelectTab(2);
                await Wait();
                ScreenshotHelper.Save(main, "grades_tab");

                // 7. Редагування оцінок і запис у БД
                var student = vm.SelectedStudent;
                var edit = new GradeEditDialog(student);
                edit.Show(main);
                edit.EditableGrades[1].Score = 88;   // Вища математика
                edit.EditableGrades[2].Score = 90;   // Фізика
                await Wait();
                ScreenshotHelper.Save(edit, "grade_edit");
                await ServerApi.SaveGradesAsync(student.Id, edit.EditableGrades);
                edit.Close(true);

                // 8. Оновлений рейтинг після запису
                main.SelectTab(0);
                await main.LoadRatingAsync();
                vm.SelectedStudent = vm.Rating.FirstOrDefault(s => s.Id == student.Id);
                await Wait();
                ScreenshotHelper.Save(main, "rating_after_save");

                // 9. Додавання студента
                var add = new AddStudentDialog(vm.Groups);
                add.Show(main);
                add.Fill("Новенький", "Студент", Math.Max(0, vm.Groups.IndexOf("КІ-301")), 3);
                await Wait();
                ScreenshotHelper.Save(add, "add_student");
                await add.SaveAsync();
                await main.InitializeDataAsync();
                vm.SelectedGroup = "КІ-301";
                await main.LoadRatingAsync();
                await Wait();
                ScreenshotHelper.Save(main, "rating_with_new_student");

                // 10. Про програму
                var about = new AboutWindow();
                about.Show(main);
                await Wait();
                ScreenshotHelper.Save(about, "about");
                about.Close();

                Console.WriteLine("Демо завершено.");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Демо перервано: " + ex);
            }
            finally
            {
                await Wait(500);
                App.Desktop?.Shutdown();
            }
        }
    }
}
