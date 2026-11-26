using System;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;

namespace StudentRating.Client
{
    /// <summary>Знімки вікон у PNG: F12 або Ctrl/Cmd+Shift+S, а також з демо-режиму.</summary>
    public static class ScreenshotHelper
    {
        public static string Folder = Path.Combine(AppContext.BaseDirectory, "screenshots");
        private static int _counter;

        public static void Attach(Window window)
        {
            window.AddHandler(InputElement.KeyDownEvent, (s, e) =>
            {
                bool combo = e.Key == Key.S && e.KeyModifiers.HasFlag(KeyModifiers.Shift)
                             && (e.KeyModifiers.HasFlag(KeyModifiers.Control) || e.KeyModifiers.HasFlag(KeyModifiers.Meta));
                if (e.Key == Key.F12 || combo)
                {
                    Save(window, window.GetType().Name);
                    e.Handled = true;
                }
            }, RoutingStrategies.Tunnel, handledEventsToo: true);
        }

        public static string Save(Window window, string name)
        {
            try
            {
                Directory.CreateDirectory(Folder);
                double scale = window.RenderScaling;
                var size = new PixelSize((int)(window.Bounds.Width * scale), (int)(window.Bounds.Height * scale));
                using (var bmp = new RenderTargetBitmap(size, new Vector(96 * scale, 96 * scale)))
                {
                    bmp.Render(window);
                    var path = Path.Combine(Folder, string.Format("{0:00}_{1}.png", ++_counter, name));
                    bmp.Save(path);
                    Console.WriteLine("Знімок: " + path);
                    return path;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Не вдалося зберегти знімок: " + ex.Message);
                return null;
            }
        }
    }
}
