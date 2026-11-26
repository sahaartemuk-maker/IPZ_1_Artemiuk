using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.Sqlite;
using StudentRating.Protocol;

namespace StudentRating.Server
{
    /// <summary>Робота з базою даних SQLite.</summary>
    public class Database
    {
        private readonly string _cs;
        private readonly object _sync = new object();

        public static readonly string[] DefaultSubjects = { "Програмування", "Вища математика", "Фізика", "Бази даних", "ІПЗ" };

        public Database(string path)
        {
            _cs = new SqliteConnectionStringBuilder { DataSource = path }.ToString();
        }

        private SqliteConnection Open()
        {
            var c = new SqliteConnection(_cs);
            c.Open();
            using (var pragma = c.CreateCommand()) { pragma.CommandText = "PRAGMA foreign_keys = ON;"; pragma.ExecuteNonQuery(); }
            return c;
        }

        public void Initialize()
        {
            using (var c = Open())
            {
                Exec(c, @"
CREATE TABLE IF NOT EXISTS Users(
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Login TEXT NOT NULL UNIQUE,
    PasswordHash TEXT NOT NULL,
    FullName TEXT NOT NULL,
    Role TEXT NOT NULL CHECK(Role IN ('Student','Teacher','Admin')));
CREATE TABLE IF NOT EXISTS Students(
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    LastName TEXT NOT NULL,
    FirstName TEXT NOT NULL,
    GroupName TEXT NOT NULL,
    Course INTEGER NOT NULL CHECK(Course BETWEEN 1 AND 6));
CREATE TABLE IF NOT EXISTS Subjects(
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL UNIQUE);
CREATE TABLE IF NOT EXISTS Grades(
    StudentId INTEGER NOT NULL REFERENCES Students(Id) ON DELETE CASCADE,
    SubjectId INTEGER NOT NULL REFERENCES Subjects(Id),
    Score INTEGER NOT NULL CHECK(Score BETWEEN 0 AND 100),
    UpdatedAt TEXT NOT NULL,
    PRIMARY KEY(StudentId, SubjectId));
CREATE TABLE IF NOT EXISTS RequestLog(
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Time TEXT NOT NULL,
    Client TEXT NOT NULL,
    Command TEXT NOT NULL,
    Status TEXT NOT NULL);");

                if (Scalar<long>(c, "SELECT COUNT(*) FROM Users") == 0) Seed(c);
            }
        }

        private void Seed(SqliteConnection c)
        {
            Logger.Info("Порожня БД — заповнюю демонстраційними даними");
            using (var tx = c.BeginTransaction())
            {
                AddUser(c, tx, "student", "1111", "Артемюк Олександр", "Student");
                AddUser(c, tx, "teacher", "2222", "Викладач кафедри ЕОМ", "Teacher");
                AddUser(c, tx, "admin", "admin", "Адміністратор", "Admin");
                foreach (var s in DefaultSubjects)
                    Exec(c, "INSERT INTO Subjects(Name) VALUES($n)", tx, ("$n", s));

                var names = new[]
                {
                    "Артемюк Олександр", "Коваль Марія", "Шевчук Андрій", "Бондар Ірина", "Ткаченко Дмитро",
                    "Кравець Олена", "Мельник Тарас", "Поліщук Юлія", "Лисенко Максим", "Гнатюк Софія",
                    "Савчук Віктор", "Романюк Анна", "Олійник Богдан", "Карпенко Наталія", "Зайченко Ігор"
                };
                var groups = new[] { "КІ-301", "КІ-302", "КІ-201" };
                var rnd = new Random(301);
                for (int i = 0; i < names.Length; i++)
                {
                    var p = names[i].Split(' ');
                    var g = groups[i % groups.Length];
                    Exec(c, "INSERT INTO Students(LastName,FirstName,GroupName,Course) VALUES($l,$f,$g,$c)", tx,
                        ("$l", p[0]), ("$f", p[1]), ("$g", g), ("$c", g[3] - '0'));
                    long sid = Scalar<long>(c, "SELECT last_insert_rowid()", tx);
                    for (int j = 1; j <= DefaultSubjects.Length; j++)
                        Exec(c, "INSERT INTO Grades VALUES($s,$j,$v,$t)", tx,
                            ("$s", sid), ("$j", j), ("$v", rnd.Next(55, 101)), ("$t", Now()));
                }
                tx.Commit();
            }
        }

        // ---------- API для обробника команд ----------

        public UserDto CheckLogin(string login, string password)
        {
            lock (_sync)
            using (var c = Open())
            using (var cmd = c.CreateCommand())
            {
                cmd.CommandText = "SELECT Login, FullName, Role FROM Users WHERE Login=$l AND PasswordHash=$p";
                cmd.Parameters.AddWithValue("$l", login);
                cmd.Parameters.AddWithValue("$p", Hash(password));
                using (var r = cmd.ExecuteReader())
                    return r.Read() ? new UserDto { Login = r.GetString(0), FullName = r.GetString(1), Role = r.GetString(2) } : null;
            }
        }

        public List<string> GetGroups()
        {
            lock (_sync)
            using (var c = Open())
                return Query(c, "SELECT DISTINCT GroupName FROM Students ORDER BY GroupName", r => r.GetString(0));
        }

        public List<string> GetSubjects()
        {
            lock (_sync)
            using (var c = Open())
                return Query(c, "SELECT Name FROM Subjects ORDER BY Id", r => r.GetString(0));
        }

        /// <summary>Студенти з оцінками за фільтром (група/курс).</summary>
        public List<StudentDto> GetStudents(string group = null, int? course = null, int? id = null)
        {
            lock (_sync)
            using (var c = Open())
            {
                var where = new List<string>();
                var prm = new List<(string, object)>();
                if (!string.IsNullOrEmpty(group)) { where.Add("s.GroupName=$g"); prm.Add(("$g", group)); }
                if (course.HasValue) { where.Add("s.Course=$c"); prm.Add(("$c", course.Value)); }
                if (id.HasValue) { where.Add("s.Id=$id"); prm.Add(("$id", id.Value)); }
                var sql = @"SELECT s.Id, s.LastName, s.FirstName, s.GroupName, s.Course, sub.Name, g.Score
                            FROM Students s
                            LEFT JOIN Grades g ON g.StudentId = s.Id
                            LEFT JOIN Subjects sub ON sub.Id = g.SubjectId"
                          + (where.Count > 0 ? " WHERE " + string.Join(" AND ", where) : "")
                          + " ORDER BY s.Id, sub.Id";

                var result = new Dictionary<int, StudentDto>();
                using (var cmd = c.CreateCommand())
                {
                    cmd.CommandText = sql;
                    foreach (var (k, v) in prm) cmd.Parameters.AddWithValue(k, v);
                    using (var r = cmd.ExecuteReader())
                        while (r.Read())
                        {
                            int sid = r.GetInt32(0);
                            if (!result.TryGetValue(sid, out var st))
                                result[sid] = st = new StudentDto
                                {
                                    Id = sid, LastName = r.GetString(1), FirstName = r.GetString(2),
                                    Group = r.GetString(3), Course = r.GetInt32(4)
                                };
                            if (!r.IsDBNull(5)) st.Grades.Add(new GradeDto { Subject = r.GetString(5), Score = r.GetInt32(6) });
                        }
                }
                foreach (var st in result.Values)
                    st.AverageScore = st.Grades.Count == 0 ? 0 : Math.Round(st.Grades.Average(x => x.Score), 2);
                return result.Values.ToList();
            }
        }

        /// <summary>Записати оцінки студента (в одній транзакції).</summary>
        public int SaveGrades(int studentId, IList<GradeDto> grades)
        {
            lock (_sync)
            using (var c = Open())
            using (var tx = c.BeginTransaction())
            {
                if (Scalar<long>(c, "SELECT COUNT(*) FROM Students WHERE Id=$id", tx, ("$id", studentId)) == 0)
                    throw new ArgumentException($"Студента з Id={studentId} не існує");
                int n = 0;
                foreach (var g in grades)
                {
                    if (g.Score < 0 || g.Score > 100)
                        throw new ArgumentOutOfRangeException(nameof(g.Score), $"Оцінка {g.Score} поза діапазоном 0..100");
                    var subjectId = Scalar<long?>(c, "SELECT Id FROM Subjects WHERE Name=$n", tx, ("$n", g.Subject));
                    if (subjectId == null) throw new ArgumentException($"Невідомий предмет «{g.Subject}»");
                    n += Exec(c, @"INSERT INTO Grades VALUES($s,$j,$v,$t)
                                   ON CONFLICT(StudentId,SubjectId) DO UPDATE SET Score=excluded.Score, UpdatedAt=excluded.UpdatedAt",
                        tx, ("$s", studentId), ("$j", subjectId.Value), ("$v", g.Score), ("$t", Now()));
                }
                tx.Commit();   // якщо був виняток — транзакція відкотиться при Dispose
                return n;
            }
        }

        public int AddStudent(StudentDto s)
        {
            if (string.IsNullOrWhiteSpace(s.LastName) || string.IsNullOrWhiteSpace(s.FirstName) || string.IsNullOrWhiteSpace(s.Group))
                throw new ArgumentException("Прізвище, ім'я та група обов'язкові");
            lock (_sync)
            using (var c = Open())
            {
                Exec(c, "INSERT INTO Students(LastName,FirstName,GroupName,Course) VALUES($l,$f,$g,$c)", null,
                    ("$l", s.LastName), ("$f", s.FirstName), ("$g", s.Group), ("$c", s.Course));
                return (int)Scalar<long>(c, "SELECT last_insert_rowid()");
            }
        }

        public void LogRequest(string client, string command, string status)
        {
            try
            {
                lock (_sync)
                using (var c = Open())
                    Exec(c, "INSERT INTO RequestLog(Time,Client,Command,Status) VALUES($t,$c,$m,$s)", null,
                        ("$t", Now()), ("$c", client), ("$m", command), ("$s", status));
            }
            catch (SqliteException ex) { Logger.Error("Журнал запитів: " + ex.Message); }
        }

        // ---------- допоміжні ----------

        private static void AddUser(SqliteConnection c, SqliteTransaction tx, string login, string pwd, string name, string role)
            => Exec(c, "INSERT INTO Users(Login,PasswordHash,FullName,Role) VALUES($l,$p,$n,$r)", tx,
                ("$l", login), ("$p", Hash(pwd)), ("$n", name), ("$r", role));

        public static string Hash(string s)
        {
            using (var sha = SHA256.Create())
                return Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(s ?? "")));
        }

        private static string Now() => DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        private static int Exec(SqliteConnection c, string sql, SqliteTransaction tx = null, params (string, object)[] p)
        {
            using (var cmd = c.CreateCommand())
            {
                cmd.CommandText = sql; cmd.Transaction = tx;
                foreach (var (k, v) in p) cmd.Parameters.AddWithValue(k, v);
                return cmd.ExecuteNonQuery();
            }
        }

        private static T Scalar<T>(SqliteConnection c, string sql, SqliteTransaction tx = null, params (string, object)[] p)
        {
            using (var cmd = c.CreateCommand())
            {
                cmd.CommandText = sql; cmd.Transaction = tx;
                foreach (var (k, v) in p) cmd.Parameters.AddWithValue(k, v);
                var o = cmd.ExecuteScalar();
                if (o == null || o is DBNull) return default(T);
                var t = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
                return (T)Convert.ChangeType(o, t);
            }
        }

        private static List<T> Query<T>(SqliteConnection c, string sql, Func<SqliteDataReader, T> map)
        {
            var list = new List<T>();
            using (var cmd = c.CreateCommand())
            {
                cmd.CommandText = sql;
                using (var r = cmd.ExecuteReader()) while (r.Read()) list.Add(map(r));
            }
            return list;
        }
    }
}
