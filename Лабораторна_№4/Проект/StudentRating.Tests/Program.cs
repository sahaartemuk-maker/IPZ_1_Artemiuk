using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using StudentRating.Protocol;

namespace StudentRating.Tests
{
    /// <summary>
    /// Набір тестових процедур для перевірки всієї системи через TCP-протокол.
    /// Використання: StudentRating.Tests [host] [port] [файл_результатів]
    /// </summary>
    internal static class Program
    {
        private class Conn : IDisposable
        {
            private readonly TcpClient _tcp;
            private readonly PacketChannel _ch;
            public string Token;
            public Conn(string host, int port) { _tcp = new TcpClient(host, port); _ch = new PacketChannel(_tcp.GetStream()); }
            public JsonElement Raw(string line) { _ch.WriteLine(line); return JsonDocument.Parse(_ch.ReadLine()).RootElement.Clone(); }
            public JsonElement Call(string cmd, object args = null)
                => Raw(JsonSerializer.Serialize(new { Command = cmd, Token, Args = args ?? new { } }, PacketChannel.Json));
            public void Dispose() { _tcp.Dispose(); }
        }

        private class Result { public string Id, Name, Expected, Actual; public bool Pass; }

        private static string Host = "127.0.0.1";
        private static int Port = 5050;
        private static readonly List<Result> Results = new List<Result>();

        private static string St(JsonElement r) => r.GetProperty("Status").GetString();
        private static string Msg(JsonElement r) => r.TryGetProperty("Message", out var m) && m.ValueKind == JsonValueKind.String ? m.GetString() : "";

        private static void Test(string id, string name, string expected, Func<(bool ok, string actual)> body)
        {
            Result res;
            try
            {
                var (ok, actual) = body();
                res = new Result { Id = id, Name = name, Expected = expected, Actual = actual, Pass = ok };
            }
            catch (Exception ex)
            {
                res = new Result { Id = id, Name = name, Expected = expected, Actual = "Виняток: " + ex.Message, Pass = false };
            }
            Results.Add(res);
            var old = Console.ForegroundColor;
            Console.ForegroundColor = res.Pass ? ConsoleColor.Green : ConsoleColor.Red;
            Console.Write(res.Pass ? "[PASS] " : "[FAIL] ");
            Console.ForegroundColor = old;
            Console.WriteLine($"{id} {name}\n        очікувано: {expected}\n        отримано:  {res.Actual}");
        }

        private static int Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            if (args.Length > 0) Host = args[0];
            if (args.Length > 1) Port = int.Parse(args[1]);
            string outFile = args.Length > 2 ? args[2] : "tests.json";

            Console.WriteLine($"Тестування сервера {Host}:{Port}\n");
            try
            {
                using (var anon = new Conn(Host, Port))
                using (var stud = new Conn(Host, Port))
                using (var t = new Conn(Host, Port))
                {
                    Run(anon, stud, t);
                }
            }
            catch (SocketException ex)
            {
                Console.WriteLine("Сервер недоступний: " + ex.Message);
                return 2;
            }

            int pass = Results.Count(r => r.Pass);
            Console.WriteLine($"\nПідсумок: пройдено {pass} з {Results.Count}");
            File.WriteAllText(outFile, JsonSerializer.Serialize(Results.Select(r => new { r.Id, r.Name, r.Expected, r.Actual, r.Pass }),
                new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping }));
            return pass == Results.Count ? 0 : 1;
        }

        private static void Run(Conn anon, Conn stud, Conn t)
        {
            Test("T01", "Перевірка зв'язку (PING)", "OK, PONG", () =>
            {
                var r = anon.Call("PING");
                return (St(r) == "OK" && r.GetProperty("Data").GetString() == "PONG", $"{St(r)}, {r.GetProperty("Data")}");
            });

            Test("T02", "Запит рейтингу без авторизації", "ACCESS_DENIED", () =>
            {
                var r = anon.Call("GET_RATING", new { group = "КІ-301" });
                return (St(r) == "ACCESS_DENIED", $"{St(r)}: {Msg(r)}");
            });

            Test("T03", "Вхід з невірним паролем", "ACCESS_DENIED", () =>
            {
                var r = anon.Call("LOGIN", new { login = "teacher", password = "wrong" });
                return (St(r) == "ACCESS_DENIED", $"{St(r)}: {Msg(r)}");
            });

            Test("T04", "Вхід студента", "OK, роль Student, видано токен", () =>
            {
                var r = stud.Call("LOGIN", new { login = "student", password = "1111" });
                var d = r.GetProperty("Data");
                stud.Token = d.GetProperty("Token").GetString();
                return (St(r) == "OK" && d.GetProperty("Role").GetString() == "Student" && stud.Token.Length == 32,
                        $"{St(r)}, роль {d.GetProperty("Role")}, токен {stud.Token.Length} симв.");
            });

            Test("T05", "Студент намагається змінити оцінки", "ACCESS_DENIED", () =>
            {
                var r = stud.Call("SAVE_GRADES", new { studentId = 1, grades = new[] { new { Subject = "ІПЗ", Score = 100 } } });
                return (St(r) == "ACCESS_DENIED", $"{St(r)}: {Msg(r)}");
            });

            Test("T06", "Вхід викладача", "OK, роль Teacher", () =>
            {
                var r = t.Call("LOGIN", new { login = "teacher", password = "2222" });
                t.Token = r.GetProperty("Data").GetProperty("Token").GetString();
                return (St(r) == "OK" && r.GetProperty("Data").GetProperty("Role").GetString() == "Teacher",
                        $"{St(r)}, роль {r.GetProperty("Data").GetProperty("Role")}");
            });

            Test("T07", "Довідники груп і предметів", "3 групи, 5 предметів", () =>
            {
                var d = t.Call("GET_GROUPS").GetProperty("Data");
                int g = d.GetProperty("Groups").GetArrayLength(), s = d.GetProperty("Subjects").GetArrayLength();
                return (g == 3 && s == 5, $"{g} груп, {s} предметів");
            });

            Test("T08", "Рейтинг групи КІ-301 відсортовано", "5 студентів, бал спадає, місця 1..5", () =>
            {
                var list = t.Call("GET_RATING", new { group = "КІ-301" }).GetProperty("Data").EnumerateArray().ToList();
                var avg = list.Select(x => x.GetProperty("AverageScore").GetDouble()).ToList();
                var places = list.Select(x => x.GetProperty("Place").GetInt32()).ToList();
                bool sorted = avg.Zip(avg.Skip(1), (a, b) => a >= b).All(x => x);
                return (list.Count == 5 && sorted && places.SequenceEqual(Enumerable.Range(1, list.Count)),
                        $"{list.Count} студентів, бали: {string.Join(" ≥ ", avg)}");
            });

            Test("T09", "Рейтинг неіснуючої групи", "OK, порожній список", () =>
            {
                var r = t.Call("GET_RATING", new { group = "XX-999" });
                return (St(r) == "OK" && r.GetProperty("Data").GetArrayLength() == 0, $"{St(r)}, {r.GetProperty("Data").GetArrayLength()} записів");
            });

            Test("T10", "Пошук кращих: 3 курс, «Програмування» ≥ 70, топ-3", "≤ 3 студенти, усі з 3 курсу", () =>
            {
                var list = t.Call("FIND_BEST", new { course = 3, subject = "Програмування", minScore = 70, top = 3 })
                            .GetProperty("Data").EnumerateArray().ToList();
                var scores = list.Select(x => x.GetProperty("Grades").EnumerateArray()
                                 .First(g => g.GetProperty("Subject").GetString() == "Програмування").GetProperty("Score").GetInt32()).ToList();
                bool ok = list.Count <= 3 && list.Count > 0 && list.All(x => x.GetProperty("Course").GetInt32() == 3)
                          && scores.All(s => s >= 70) && scores.Zip(scores.Skip(1), (a, b) => a >= b).All(x => x);
                return (ok, $"{list.Count} студ., бали з програмування: {string.Join(", ", scores)}");
            });

            Test("T11", "Запис оцінки в БД і повторне читання", "OK; GET_STUDENT повертає 77 з фізики", () =>
            {
                var r = t.Call("SAVE_GRADES", new { studentId = 2, grades = new[] { new { Subject = "Фізика", Score = 77 } } });
                var s = t.Call("GET_STUDENT", new { id = 2 }).GetProperty("Data");
                int v = s.GetProperty("Grades").EnumerateArray().First(g => g.GetProperty("Subject").GetString() == "Фізика").GetProperty("Score").GetInt32();
                return (St(r) == "OK" && v == 77, $"{St(r)} ({Msg(r)}); у БД: {v}");
            });

            Test("T12", "Оцінка поза діапазоном (150) — відкат транзакції", "ERROR; у БД нічого не змінилось", () =>
            {
                int ipzBefore = t.Call("GET_STUDENT", new { id = 2 }).GetProperty("Data").GetProperty("Grades").EnumerateArray()
                                 .First(g => g.GetProperty("Subject").GetString() == "ІПЗ").GetProperty("Score").GetInt32();
                var r = t.Call("SAVE_GRADES", new
                {
                    studentId = 2,
                    grades = new[] { new { Subject = "ІПЗ", Score = 1 }, new { Subject = "Фізика", Score = 150 } }
                });
                var s = t.Call("GET_STUDENT", new { id = 2 }).GetProperty("Data");
                int phys = s.GetProperty("Grades").EnumerateArray().First(g => g.GetProperty("Subject").GetString() == "Фізика").GetProperty("Score").GetInt32();
                int ipz = s.GetProperty("Grades").EnumerateArray().First(g => g.GetProperty("Subject").GetString() == "ІПЗ").GetProperty("Score").GetInt32();
                return (St(r) == "ERROR" && phys == 77 && ipz == ipzBefore, $"{St(r)}: {Msg(r)}; Фізика={phys}, ІПЗ={ipz} (було {ipzBefore})");
            });

            Test("T13", "Оцінка з невідомого предмета", "ERROR", () =>
            {
                var r = t.Call("SAVE_GRADES", new { studentId = 2, grades = new[] { new { Subject = "Астрологія", Score = 90 } } });
                return (St(r) == "ERROR", $"{St(r)}: {Msg(r)}");
            });

            Test("T14", "Оцінки неіснуючому студенту", "ERROR", () =>
            {
                var r = t.Call("SAVE_GRADES", new { studentId = 9999, grades = new[] { new { Subject = "ІПЗ", Score = 90 } } });
                return (St(r) == "ERROR", $"{St(r)}: {Msg(r)}");
            });

            Test("T15", "Додавання студента і читання з БД", "OK, новий Id, GET_STUDENT знаходить", () =>
            {
                var r = t.Call("ADD_STUDENT", new { lastName = "Тестовий", firstName = "Студент", group = "КІ-302", course = 3 });
                int id = r.GetProperty("Data").GetProperty("Id").GetInt32();
                var s = t.Call("GET_STUDENT", new { id }).GetProperty("Data");
                return (St(r) == "OK" && s.GetProperty("LastName").GetString() == "Тестовий", $"{St(r)}, Id={id}, {s.GetProperty("LastName")} {s.GetProperty("Group")}");
            });

            Test("T16", "Додавання студента без прізвища", "ERROR", () =>
            {
                var r = t.Call("ADD_STUDENT", new { lastName = "", firstName = "Х", group = "КІ-301", course = 3 });
                return (St(r) == "ERROR", $"{St(r)}: {Msg(r)}");
            });

            Test("T17", "Невідома команда", "UNKNOWN_COMMAND", () =>
            {
                var r = t.Call("DROP_DATABASE");
                return (St(r) == "UNKNOWN_COMMAND", $"{St(r)}: {Msg(r)}");
            });

            Test("T18", "Некоректний пакет (не JSON)", "ERROR, з'єднання не розривається", () =>
            {
                var r = t.Raw("hello server");
                var p = t.Call("PING");
                return (St(r) == "ERROR" && St(p) == "OK", $"{St(r)}: {Msg(r)}; наступний PING: {St(p)}");
            });

            Test("T19", "5 одночасних клієнтів", "усі отримали PONG", () =>
            {
                var tasks = Enumerable.Range(0, 5).Select(_ => Task.Run(() =>
                {
                    using (var c = new Conn(Host, Port)) return c.Call("PING").GetProperty("Data").GetString();
                })).ToArray();
                Task.WaitAll(tasks);
                int ok = tasks.Count(x => x.Result == "PONG");
                return (ok == 5, $"{ok} з 5 відповіли PONG");
            });

            Test("T20", "Підробний токен", "ACCESS_DENIED", () =>
            {
                var saved = t.Token; t.Token = "00000000000000000000000000000000";
                var r = t.Call("GET_GROUPS"); t.Token = saved;
                return (St(r) == "ACCESS_DENIED", $"{St(r)}: {Msg(r)}");
            });
        }
    }
}
