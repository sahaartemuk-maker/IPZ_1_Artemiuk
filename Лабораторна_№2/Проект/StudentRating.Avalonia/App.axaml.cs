using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using StudentRating.Client.Models;
using StudentRating.Client.Views;

namespace StudentRating.Client
{
    public partial class App : Application
    {
        /// <summary>Поточний авторизований користувач.</summary>
        public static User CurrentUser { get; set; }

        public static IClassicDesktopStyleApplicationLifetime Desktop
        {
            get { return Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime; }
        }

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                desktop.MainWindow = new LoginWindow();
            base.OnFrameworkInitializationCompleted();
        }
    }
}
