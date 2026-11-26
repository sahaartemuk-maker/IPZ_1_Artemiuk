using System;
using System.Threading;

namespace StudentRating.Server
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            int port = 5050;
            string dbPath = "rating.db";
            try
            {
                if (args.Length > 0) port = int.Parse(args[0]);
                if (args.Length > 1) dbPath = args[1];

                var db = new Database(dbPath);
                db.Initialize();
                Logger.Info($"БД: {System.IO.Path.GetFullPath(dbPath)}");

                var server = new TcpServer(port, new CommandHandler(db), db);
                var stop = new ManualResetEvent(false);
                Console.CancelKeyPress += (s, e) => { e.Cancel = true; stop.Set(); };

                server.Start();
                Logger.Info("Сервер працює. Ctrl+C — зупинити.");
                stop.WaitOne();
                server.Stop();
                Logger.Info("Сервер зупинено.");
            }
            catch (FormatException)
            {
                Console.Error.WriteLine("Невірний номер порту. Використання: StudentRating.Server [порт] [файл_БД]");
            }
            catch (Exception ex)
            {
                Logger.Error("Критична помилка: " + ex.Message);
            }
        }
    }
}
