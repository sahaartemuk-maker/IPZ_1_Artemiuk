using System;
using System.IO;

namespace StudentRating.Server
{
    /// <summary>Журнал подій: консоль + файл server.log.</summary>
    public static class Logger
    {
        private static readonly object Sync = new object();
        public static string FilePath = "server.log";

        public static void Info(string msg) => Write("INFO", msg, ConsoleColor.Gray);
        public static void In(string client, string msg) => Write("<<", $"[{client}] {msg}", ConsoleColor.Cyan);
        public static void Out(string client, string msg) => Write(">>", $"[{client}] {msg}", ConsoleColor.Green);
        public static void Error(string msg) => Write("ERR", msg, ConsoleColor.Red);

        private static void Write(string level, string msg, ConsoleColor color)
        {
            var line = $"{DateTime.Now:HH:mm:ss} {level,-4} {msg}";
            lock (Sync)
            {
                var old = Console.ForegroundColor;
                Console.ForegroundColor = color;
                Console.WriteLine(line);
                Console.ForegroundColor = old;
                try { File.AppendAllText(FilePath, line + Environment.NewLine); }
                catch (IOException) { /* журнал у файл не критичний */ }
            }
        }
    }
}
