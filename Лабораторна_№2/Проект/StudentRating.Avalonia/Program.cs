using System;
using Avalonia;

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
                if (args.Length > 0) ScreenshotHelper.Folder = args[0];
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
