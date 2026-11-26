using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using StudentRating.Client.Models;
using StudentRating.Protocol;

namespace StudentRating.Client.Services
{
    /// <summary>Помилка, яку повернув сервер (Status != OK).</summary>
    public class ServerException : Exception
    {
        public string Status { get; private set; }
        public ServerException(string status, string message) : base(message) { Status = status; }
    }

    /// <summary>Клієнт API сервера: одне TCP-з'єднання, запити по черзі.</summary>
    public static class ServerApi
    {
        private static TcpClient _tcp;
        private static StreamReader _reader;
        private static StreamWriter _writer;
        private static string _token;
        private static readonly SemaphoreSlim Gate = new SemaphoreSlim(1, 1);

        public static string Host { get; set; } = "127.0.0.1";
        public static int Port { get; set; } = 5050;
        public static List<string> Subjects { get; private set; } = new List<string>();

        public static bool IsConnected { get { return _tcp != null && _tcp.Connected; } }

        public static async Task ConnectAsync()
        {
            Disconnect();
            var tcp = new TcpClient();
            var connect = tcp.ConnectAsync(Host, Port);
            if (await Task.WhenAny(connect, Task.Delay(3000)) != connect)
            {
                tcp.Dispose();
                throw new TimeoutException($"Сервер {Host}:{Port} не відповідає");
            }
            await connect;   // прокидає SocketException
            _tcp = tcp;
            var stream = tcp.GetStream();
            _reader = new StreamReader(stream, new System.Text.UTF8Encoding(false));
            _writer = new StreamWriter(stream, new System.Text.UTF8Encoding(false)) { AutoFlush = true, NewLine = "\n" };
        }

        public static void Disconnect()
        {
            _token = null;
            try { _tcp?.Dispose(); } catch (Exception) { }
            _tcp = null;
        }

        private static async Task<JsonElement> Call(string command, object args = null)
        {
            if (!IsConnected) await ConnectAsync();
            await Gate.WaitAsync();
            try
            {
                var json = JsonSerializer.Serialize(new { Command = command, Token = _token, Args = args ?? new { } }, PacketChannel.Json);
                await _writer.WriteLineAsync(json);
                var line = await _reader.ReadLineAsync();
                if (line == null)
                {
                    Disconnect();
                    throw new IOException("Сервер розірвав з'єднання");
                }
                var root = JsonDocument.Parse(line).RootElement;
                var status = root.GetProperty("Status").GetString();
                if (status != Statuses.Ok)
                {
                    var msg = root.TryGetProperty("Message", out var m) && m.ValueKind == JsonValueKind.String ? m.GetString() : status;
                    throw new ServerException(status, msg);
                }
                return root.GetProperty("Data").Clone();
            }
            finally { Gate.Release(); }
        }

        private static T As<T>(JsonElement e) { return e.Deserialize<T>(PacketChannel.Json); }

        // ---------- API ----------

        public static async Task<User> LoginAsync(string login, string password)
        {
            if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Введіть логін і пароль.");
            var dto = As<UserDto>(await Call(Commands.Login, new { login, password }));
            _token = dto.Token;
            UserRole role;
            Enum.TryParse(dto.Role, out role);
            return new User { Login = dto.Login, FullName = dto.FullName, Role = role };
        }

        public static async Task<List<string>> GetGroupsAsync()
        {
            var data = await Call(Commands.GetGroups);
            Subjects = As<List<string>>(data.GetProperty("Subjects"));
            return As<List<string>>(data.GetProperty("Groups"));
        }

        public static async Task<List<Student>> GetRatingAsync(string group)
        {
            if (string.IsNullOrEmpty(group)) throw new ArgumentException("Оберіть групу.");
            return As<List<StudentDto>>(await Call(Commands.GetRating, new { group })).Select(ToModel).ToList();
        }

        public static async Task<List<Student>> FindBestAsync(SearchCriteria c)
        {
            var args = new
            {
                course = c.Course,
                group = string.IsNullOrEmpty(c.Group) ? null : c.Group,
                subject = string.IsNullOrEmpty(c.Subject) ? null : c.Subject,
                minScore = (int)c.MinScore,
                top = c.TopCount
            };
            return As<List<StudentDto>>(await Call(Commands.FindBest, args)).Select(ToModel).ToList();
        }

        public static async Task<Student> GetStudentAsync(int id)
        {
            return ToModel(As<StudentDto>(await Call(Commands.GetStudent, new { id })));
        }

        public static async Task SaveGradesAsync(int studentId, IEnumerable<Grade> grades)
        {
            var list = grades.Select(g => new GradeDto { Subject = g.Subject, Score = g.Score }).ToList();
            await Call(Commands.SaveGrades, new { studentId, grades = list });
        }

        public static async Task<int> AddStudentAsync(string lastName, string firstName, string group, int course)
        {
            var data = await Call(Commands.AddStudent, new { lastName, firstName, group, course });
            return data.GetProperty("Id").GetInt32();
        }

        private static Student ToModel(StudentDto d)
        {
            var s = new Student { Id = d.Id, LastName = d.LastName, FirstName = d.FirstName, Group = d.Group, Course = d.Course, Place = d.Place };
            foreach (var g in d.Grades) s.Grades.Add(new Grade { Subject = g.Subject, Score = g.Score });
            return s;
        }
    }
}
