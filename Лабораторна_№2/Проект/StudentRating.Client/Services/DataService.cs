using System;
using System.Collections.Generic;
using System.Linq;
using StudentRating.Client.Models;

namespace StudentRating.Client.Services
{
    /// <summary>
    /// Тимчасове джерело даних для GUI (ЛР2).
    /// У ЛР3 буде замінене на обмін із сервером за протоколом TCP.
    /// </summary>
    public static class DataService
    {
        public static readonly string[] Subjects = { "Програмування", "Вища математика", "Фізика", "Бази даних", "ІПЗ" };

        private static readonly List<Student> _students = CreateDemoStudents();

        public static User Login(string login, string password)
        {
            if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Введіть логін і пароль.");

            // Демо-облікові записи (до появи сервера)
            if (login == "student" && password == "1111")
                return new User { Login = login, FullName = "Артемюк Олександр", Role = UserRole.Student };
            if (login == "teacher" && password == "2222")
                return new User { Login = login, FullName = "Викладач кафедри ЕОМ", Role = UserRole.Teacher };
            if (login == "admin" && password == "admin")
                return new User { Login = login, FullName = "Адміністратор", Role = UserRole.Admin };

            throw new UnauthorizedAccessException("Невірний логін або пароль.");
        }

        public static IEnumerable<string> GetGroups()
        {
            return _students.Select(s => s.Group).Distinct().OrderBy(g => g);
        }

        public static List<Student> GetRating(string group)
        {
            if (string.IsNullOrEmpty(group))
                throw new ArgumentException("Оберіть групу.");
            var list = _students.Where(s => s.Group == group)
                                .OrderByDescending(s => s.AverageScore).ToList();
            for (int i = 0; i < list.Count; i++) list[i].Place = i + 1;
            return list;
        }

        public static List<Student> FindBest(SearchCriteria c)
        {
            if (c == null) throw new ArgumentNullException(nameof(c));
            IEnumerable<Student> q = _students;
            if (c.Course.HasValue) q = q.Where(s => s.Course == c.Course.Value);
            if (!string.IsNullOrEmpty(c.Group)) q = q.Where(s => s.Group == c.Group);

            Func<Student, double> score = s => string.IsNullOrEmpty(c.Subject)
                ? s.AverageScore
                : s.Grades.Where(g => g.Subject == c.Subject).Select(g => (double)g.Score).DefaultIfEmpty(0).First();

            var list = q.Where(s => score(s) >= c.MinScore)
                        .OrderByDescending(score).Take(c.TopCount).ToList();
            for (int i = 0; i < list.Count; i++) list[i].Place = i + 1;
            return list;
        }

        public static void SaveGrades(Student student)
        {
            if (student == null) throw new ArgumentNullException(nameof(student));
            if (student.Grades.Any(g => g.Score < 0 || g.Score > 100))
                throw new InvalidOperationException("Є оцінки поза діапазоном 0..100.");
            student.RefreshAverage();
        }

        private static List<Student> CreateDemoStudents()
        {
            var rnd = new Random(301);
            var names = new[]
            {
                "Артемюк Олександр", "Коваль Марія", "Шевчук Андрій", "Бондар Ірина", "Ткаченко Дмитро",
                "Кравець Олена", "Мельник Тарас", "Поліщук Юлія", "Лисенко Максим", "Гнатюк Софія",
                "Савчук Віктор", "Романюк Анна", "Олійник Богдан", "Карпенко Наталія", "Зайченко Ігор"
            };
            var groups = new[] { "КІ-301", "КІ-302", "КІ-201" };
            var result = new List<Student>();
            for (int i = 0; i < names.Length; i++)
            {
                var parts = names[i].Split(' ');
                var group = groups[i % groups.Length];
                var st = new Student
                {
                    Id = i + 1, LastName = parts[0], FirstName = parts[1],
                    Group = group, Course = group[3] - '0'
                };
                foreach (var subj in Subjects)
                    st.Grades.Add(new Grade { Subject = subj, Score = rnd.Next(55, 101) });
                result.Add(st);
            }
            return result;
        }
    }
}
