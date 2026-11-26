using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using StudentRating.Protocol;

namespace StudentRating.TestClient
{
    /// <summary>
    /// Тестовий консольний клієнт: надсилає серверу послідовність команд і виводить відповіді.
    /// Використання: StudentRating.TestClient [host] [port]
    /// </summary>
    internal static class Program
    {
        private static readonly StringBuilder Log = new StringBuilder();

        private static int Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            string host = args.Length > 0 ? args[0] : "127.0.0.1";
            int port = args.Length > 1 ? int.Parse(args[1]) : 5050;
            try
            {
                using (var tcp = new TcpClient(host, port))
                {
                    var ch = new PacketChannel(tcp.GetStream());
                    Print($"Підключено до {host}:{port}\n");

                    Send(ch, "1. Перевірка зв'язку", new { Command = "PING" });
                    Send(ch, "2. Запит без авторизації (очікується ACCESS_DENIED)", new { Command = "GET_RATING", Args = new { group = "КІ-301" } });
                    Send(ch, "3. Невірний пароль", new { Command = "LOGIN", Args = new { login = "teacher", password = "0000" } });
                    var login = Send(ch, "4. Вхід викладача", new { Command = "LOGIN", Args = new { login = "teacher", password = "2222" } });
                    var token = login.RootElement.GetProperty("Data").GetProperty("Token").GetString();

                    Send(ch, "5. Список груп і предметів", new { Command = "GET_GROUPS", Token = token });
                    Send(ch, "6. Рейтинг групи КІ-301", new { Command = "GET_RATING", Token = token, Args = new { group = "КІ-301" } });
                    Send(ch, "7. Пошук кращих: 3 курс, «Програмування» ≥ 70, топ-3",
                        new { Command = "FIND_BEST", Token = token, Args = new { course = 3, subject = "Програмування", minScore = 70, top = 3 } });
                    Send(ch, "8. Запис оцінок у БД (студент Id=1)", new
                    {
                        Command = "SAVE_GRADES", Token = token,
                        Args = new { studentId = 1, grades = new[] { new { Subject = "Програмування", Score = 98 }, new { Subject = "ІПЗ", Score = 95 } } }
                    });
                    Send(ch, "9. Перевірка: картка студента Id=1 з БД", new { Command = "GET_STUDENT", Token = token, Args = new { id = 1 } });
                    Send(ch, "10. Некоректна оцінка 150 (транзакція відкочується)", new
                    {
                        Command = "SAVE_GRADES", Token = token,
                        Args = new { studentId = 1, grades = new[] { new { Subject = "Фізика", Score = 150 } } }
                    });
                    Send(ch, "11. Невідома команда", new { Command = "DELETE_ALL", Token = token });
                }
                Print("З'єднання закрито.");
                return 0;
            }
            catch (SocketException ex)
            {
                Print($"Не вдалося підключитися до {host}:{port}: {ex.Message}. Чи запущено сервер?");
                return 1;
            }
            catch (Exception ex)
            {
                Print("Помилка: " + ex.Message);
                return 1;
            }
            finally
            {
                try { File.WriteAllText("client.log", Log.ToString()); } catch (IOException) { }
            }
        }

        private static JsonDocument Send(PacketChannel ch, string title, object request)
        {
            var json = JsonSerializer.Serialize(request, PacketChannel.Json);
            Print("=== " + title);
            Print(">> " + json);
            ch.WriteLine(json);
            var answer = ch.ReadLine() ?? throw new IOException("Сервер закрив з'єднання");
            var doc = JsonDocument.Parse(answer);
            var pretty = JsonSerializer.Serialize(doc.RootElement, new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
            Print("<< " + (pretty.Length > 1200 ? pretty.Substring(0, 1200) + "\n   …" : pretty));
            Print("");
            return doc;
        }

        private static void Print(string s)
        {
            Console.WriteLine(s);
            Log.AppendLine(s);
        }
    }
}
