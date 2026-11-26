using System;
using Avalonia;
using StudentRating.Client.Services;

namespace StudentRating.Client
{
    internal static class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            try
            {
                // Папку для знімків можна задати аргументом: dotnet run -- <папка>
                foreach (var a in args)
                {
                    if (a == "--demo") DemoRunner.Enabled = true;
                    else if (a.StartsWith("--server=")) ServerApi.Host = a.Substring(9);
                    else ScreenshotHelper.Folder = a;
                }
                BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Критична помилка: " + ex);
            }
        }

        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>().UsePlatformDetect().WithInterFont().LogToTrace();
    }
}
