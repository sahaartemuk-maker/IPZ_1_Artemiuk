using System;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media.Imaging;

namespace StudentRating.Client
{
    /// <summary>F12 — зберегти знімок поточного вікна у PNG (для звіту).</summary>
    public static class ScreenshotHelper
    {
        public static string Folder = Path.Combine(AppContext.BaseDirectory, "screenshots");
        private static int _counter;

        public static void Attach(Window window)
        {
            window.KeyDown += (s, e) =>
            {
                if (e.Key != Key.F12) return;
                try
                {
                    Directory.CreateDirectory(Folder);
                    var size = new PixelSize((int)window.Bounds.Width * 2, (int)window.Bounds.Height * 2);
                    using (var bmp = new RenderTargetBitmap(size, new Vector(192, 192)))
                    {
                        bmp.Render(window);
                        var name = string.Format("{0:00}_{1}.png", ++_counter, window.GetType().Name);
                        bmp.Save(Path.Combine(Folder, name));
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine("Не вдалося зберегти знімок: " + ex.Message);
                }
            };
        }
    }
}
