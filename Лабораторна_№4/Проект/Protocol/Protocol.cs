using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace StudentRating.Protocol
{
    /// <summary>
    /// Протокол обміну: один JSON-об'єкт на рядок (UTF-8, завершується '\n').
    /// Запит:  {"Command":"GET_RATING","Args":{"group":"КІ-301"}}
    /// Відповідь: {"Status":"OK","Message":"...","Data":...}
    /// </summary>
    public static class Commands
    {
        public const string Ping = "PING";
        public const string Login = "LOGIN";
        public const string GetGroups = "GET_GROUPS";
        public const string GetRating = "GET_RATING";
        public const string FindBest = "FIND_BEST";
        public const string GetStudent = "GET_STUDENT";
        public const string SaveGrades = "SAVE_GRADES";
        public const string AddStudent = "ADD_STUDENT";
    }

    public static class Statuses
    {
        public const string Ok = "OK";
        public const string Error = "ERROR";
        public const string AccessDenied = "ACCESS_DENIED";
        public const string UnknownCommand = "UNKNOWN_COMMAND";
    }

    public class Request
    {
        public string Command { get; set; }
        public string Token { get; set; }
        public Dictionary<string, JsonElement> Args { get; set; } = new Dictionary<string, JsonElement>();
    }

    public class Response
    {
        public string Status { get; set; }
        public string Message { get; set; }
        public object Data { get; set; }

        public static Response Ok(object data = null, string message = null)
            => new Response { Status = Statuses.Ok, Data = data, Message = message };
        public static Response Fail(string status, string message)
            => new Response { Status = status, Message = message };
    }

    public class StudentDto
    {
        public int Id { get; set; }
        public string LastName { get; set; }
        public string FirstName { get; set; }
        public string Group { get; set; }
        public int Course { get; set; }
        public double AverageScore { get; set; }
        public int Place { get; set; }
        public List<GradeDto> Grades { get; set; } = new List<GradeDto>();
    }

    public class GradeDto
    {
        public string Subject { get; set; }
        public int Score { get; set; }
    }

    public class UserDto
    {
        public string Login { get; set; }
        public string FullName { get; set; }
        public string Role { get; set; }
        public string Token { get; set; }
    }

    /// <summary>Читання/запис пакетів у мережевий потік.</summary>
    public sealed class PacketChannel
    {
        public static readonly JsonSerializerOptions Json = new JsonSerializerOptions
        {
            WriteIndented = false,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
        private readonly StreamReader _reader;
        private readonly StreamWriter _writer;

        public PacketChannel(Stream stream)
        {
            _reader = new StreamReader(stream, new UTF8Encoding(false));
            _writer = new StreamWriter(stream, new UTF8Encoding(false)) { AutoFlush = true, NewLine = "\n" };
        }

        public string ReadLine() => _reader.ReadLine();
        public void WriteLine(string line) => _writer.WriteLine(line);

        public void Send<T>(T obj) => WriteLine(JsonSerializer.Serialize(obj, Json));
        public T Receive<T>() where T : class
        {
            var line = ReadLine();
            return line == null ? null : JsonSerializer.Deserialize<T>(line, Json);
        }
    }
}
