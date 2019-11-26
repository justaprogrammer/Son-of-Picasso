using System;
using Autofac;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Logging.Serilog;
using Microsoft.EntityFrameworkCore;
using SonOfPicasso.Data.Repository;
using SonOfPicasso.UI.Avalonia.Windows;
using SonOfPicasso.UI.ViewModels;

namespace SonOfPicasso.UI.Avalonia
{
    class Program
    {
        private const string ApplicationName = "SonOfPicasso";

        public static void Main(string[] args) => BuildAvaloniaApp().Start(AppMain, args);

        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .LogToDebug();

        private static void AppMain(Application app, string[] args)
        {
            var containerBuilder = AppConfiguration.Configure(Program.ApplicationName);    
            
            containerBuilder.RegisterAssemblyTypes(typeof(Program).Assembly)
                .Where(type => type.Namespace.StartsWith("SonOfPicasso.UI.Avalonia.Windows")
                               || type.Namespace.StartsWith("SonOfPicasso.UI.Avalonia.Views"))
                .AsImplementedInterfaces()
                .AsSelf();

            var container = containerBuilder.Build();
            
            AppConfiguration.ConfigureContainer(container);

            var dataContext = container.Resolve<DataContext>();
            dataContext.Database.Migrate();

            var mainWindow = container.Resolve<MainWindow>();
            mainWindow.ViewModel = container.Resolve<ApplicationViewModel>();

            app.Run(mainWindow);
        }
    }
}
