using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using StudentRating.Protocol;

namespace StudentRating.Server
{
    /// <summary>Розбір команд клієнта та формування відповідей (API сервера).</summary>
    public class CommandHandler
    {
        private readonly Database _db;
        private readonly ConcurrentDictionary<string, UserDto> _sessions = new ConcurrentDictionary<string, UserDto>();

        public CommandHandler(Database db) { _db = db; }

        public Response Handle(Request req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.Command))
                return Response.Fail(Statuses.Error, "Порожня команда");
            try
            {
                switch (req.Command.ToUpperInvariant())
                {
                    case Commands.Ping: return Response.Ok("PONG");
                    case Commands.Login: return Login(req);
                    case Commands.GetGroups: return Auth(req, u => Response.Ok(new { Groups = _db.GetGroups(), Subjects = _db.GetSubjects() }));
                    case Commands.GetRating: return Auth(req, u => GetRating(req));
                    case Commands.FindBest: return Auth(req, u => FindBest(req));
                    case Commands.GetStudent: return Auth(req, u => GetStudent(req));
                    case Commands.SaveGrades: return Auth(req, u => SaveGrades(req), teacherOnly: true);
                    case Commands.AddStudent: return Auth(req, u => AddStudent(req), teacherOnly: true);
                    default: return Response.Fail(Statuses.UnknownCommand, $"Невідома команда {req.Command}");
                }
            }
            catch (ArgumentException ex) { return Response.Fail(Statuses.Error, ex.Message); }
            catch (KeyNotFoundException ex) { return Response.Fail(Statuses.Error, ex.Message); }
            catch (FormatException ex) { return Response.Fail(Statuses.Error, "Невірний формат параметра: " + ex.Message); }
            catch (InvalidOperationException ex) { return Response.Fail(Statuses.Error, "Невірний тип параметра: " + ex.Message); }
        }

        private Response Login(Request req)
        {
            var user = _db.CheckLogin(Str(req, "login"), Str(req, "password"));
            if (user == null) return Response.Fail(Statuses.AccessDenied, "Невірний логін або пароль");
            user.Token = Guid.NewGuid().ToString("N");
            _sessions[user.Token] = user;
            return Response.Ok(user, "Вітаємо, " + user.FullName);
        }

        private Response Auth(Request req, Func<UserDto, Response> action, bool teacherOnly = false)
        {
            if (req.Token == null || !_sessions.TryGetValue(req.Token, out var user))
                return Response.Fail(Statuses.AccessDenied, "Потрібна авторизація (LOGIN)");
            if (teacherOnly && user.Role == "Student")
                return Response.Fail(Statuses.AccessDenied, "Операція доступна лише викладачу");
            return action(user);
        }

        private Response GetRating(Request req)
        {
            var group = Str(req, "group");
            if (string.IsNullOrEmpty(group)) throw new ArgumentException("Не вказано групу");
            var list = Rank(_db.GetStudents(group));
            return Response.Ok(list, $"Студентів у групі {group}: {list.Count}");
        }

        private Response FindBest(Request req)
        {
            int? course = Int(req, "course");
            string subject = Str(req, "subject");
            double min = Int(req, "minScore") ?? 0;
            int top = Int(req, "top") ?? 5;
            if (top < 1 || top > 100) throw new ArgumentException("top має бути від 1 до 100");

            Func<StudentDto, double> score = s => string.IsNullOrEmpty(subject)
                ? s.AverageScore
                : s.Grades.Where(g => g.Subject == subject).Select(g => (double)g.Score).DefaultIfEmpty(0).First();

            var list = _db.GetStudents(Str(req, "group"), course)
                          .Where(s => score(s) >= min).OrderByDescending(score).Take(top).ToList();
            for (int i = 0; i < list.Count; i++) list[i].Place = i + 1;
            return Response.Ok(list, $"Знайдено: {list.Count}");
        }

        private Response GetStudent(Request req)
        {
            int id = Int(req, "id") ?? throw new ArgumentException("Не вказано id");
            var st = _db.GetStudents(id: id).FirstOrDefault() ?? throw new KeyNotFoundException($"Студента {id} не знайдено");
            return Response.Ok(st);
        }

        private Response SaveGrades(Request req)
        {
            int id = Int(req, "studentId") ?? throw new ArgumentException("Не вказано studentId");
            if (!req.Args.TryGetValue("grades", out var g)) throw new ArgumentException("Не вказано grades");
            var grades = g.Deserialize<List<GradeDto>>(PacketChannel.Json);
            int n = _db.SaveGrades(id, grades);
            return Response.Ok(new { Saved = n }, $"Записано оцінок: {n}");
        }

        private Response AddStudent(Request req)
        {
            var s = new StudentDto
            {
                LastName = Str(req, "lastName"), FirstName = Str(req, "firstName"),
                Group = Str(req, "group"), Course = Int(req, "course") ?? 1
            };
            int id = _db.AddStudent(s);
            return Response.Ok(new { Id = id }, "Студента додано");
        }

        private static List<StudentDto> Rank(List<StudentDto> list)
        {
            list = list.OrderByDescending(s => s.AverageScore).ToList();
            for (int i = 0; i < list.Count; i++) list[i].Place = i + 1;
            return list;
        }

        private static string Str(Request r, string key)
            => r.Args != null && r.Args.TryGetValue(key, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

        private static int? Int(Request r, string key)
        {
            if (r.Args == null || !r.Args.TryGetValue(key, out var v) || v.ValueKind == JsonValueKind.Null) return null;
            return v.ValueKind == JsonValueKind.String ? int.Parse(v.GetString()) : v.GetInt32();
        }
    }
}
