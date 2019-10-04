using System;
using System.IO.Abstractions;
using System.Windows;
using Autofac;
using AutofacSerilogIntegration;
using Microsoft.EntityFrameworkCore;
using ReactiveUI;
using Serilog;
using SonOfPicasso.Core.Interfaces;
using SonOfPicasso.Core.Logging;
using SonOfPicasso.Core.Services;
using SonOfPicasso.Data.Repository;
using SonOfPicasso.Data.Services;
using SonOfPicasso.UI.ViewModels;
using SonOfPicasso.UI.Views;
using SonOfPicasso.UI.Windows;
using Splat;
using Splat.Serilog;

namespace SonOfPicasso.UI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private const string ApplicationName = "SonOfPicasso";

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var loggerConfiguration = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .Enrich.WithThreadId()
                .Enrich.With<CustomEnrichers>();

#if DEBUG
            loggerConfiguration
                .WriteTo.Debug(outputTemplate: "{Timestamp:HH:mm:ss} [{Level:u4}] ({PaddedThreadId}) {ShortSourceContext} {Message}{NewLineIfException}{Exception}{NewLine}");
#endif

            Log.Logger = loggerConfiguration.CreateLogger();

            Akavache.BlobCache.ApplicationName = ApplicationName;

            var containerBuilder = new ContainerBuilder();

            containerBuilder.RegisterType<FileSystem>()
                .As<IFileSystem>()
                .InstancePerLifetimeScope();

            containerBuilder.Register(context =>
            {
                var environmentService = context.Resolve<IEnvironmentService>();
                var fileSystem = context.Resolve<IFileSystem>();

                return BuildDbContextOptions(environmentService, fileSystem);
            }).As<DbContextOptions<DataContext>>()
                .InstancePerLifetimeScope();

            containerBuilder.RegisterType<DataContext>()
                .As<DataContext>()
                .As<IDataContext>();

            containerBuilder.RegisterAssemblyTypes(typeof(EnvironmentService).Assembly)
                .Where(type => type.Namespace.StartsWith("SonOfPicasso.Core.Services"))
                .InstancePerLifetimeScope()
                .AsImplementedInterfaces();

            containerBuilder.RegisterAssemblyTypes(typeof(UnitOfWork).Assembly)
                .Where(type => type.Namespace.StartsWith("SonOfPicasso.Data.Services"))
                .AsImplementedInterfaces();

            containerBuilder.RegisterAssemblyTypes(GetType().Assembly)
                .Where(type => type.Namespace.StartsWith("SonOfPicasso.UI.Services"))
                .InstancePerLifetimeScope()
                .AsImplementedInterfaces();

            containerBuilder.RegisterAssemblyTypes(GetType().Assembly)
                .Where(type => type.Namespace.StartsWith("SonOfPicasso.UI.Windows")
                               || type.Namespace.StartsWith("SonOfPicasso.UI.Views")
                               || type.Namespace.StartsWith("SonOfPicasso.UI.ViewModels"))
                .AsImplementedInterfaces()
                .AsSelf();

            containerBuilder.RegisterLogger();
            var container = containerBuilder.Build();
            var resolver = new AutofacDependencyResolver(container);

            Locator.SetLocator(resolver);
            Locator.CurrentMutable.InitializeReactiveUI();

            var updatedBuilder = new ContainerBuilder();

            updatedBuilder.RegisterType<ViewModelActivator>()
                .AsSelf();

            updatedBuilder.RegisterType<CommandBinderImplementation>()
                .AsImplementedInterfaces();

            resolver.UpdateComponentContext(updatedBuilder);

            Locator.CurrentMutable.RegisterPlatformBitmapLoader();
            Locator.CurrentMutable.UseSerilogFullLogger();

            SQLitePCL.Batteries_V2.Init();

            var dataContext = container.Resolve<DataContext>();
            dataContext.Database.Migrate();

            var mainWindow = container.Resolve<MainWindow>();

            mainWindow.ViewModel = container.Resolve<ApplicationViewModel>();
            mainWindow.Show();
        }

        internal static DbContextOptions<DataContext> BuildDbContextOptions(IEnvironmentService environmentService, IFileSystem fileSystem)
        {
            string databasePath = environmentService.GetEnvironmentVariable("SonOfPicasso_DatabasePath");
            if (!string.IsNullOrWhiteSpace(databasePath))
            {
                var databaseDirectory = fileSystem.Path.GetDirectoryName(databasePath);
                fileSystem.Directory.CreateDirectory(databaseDirectory);
            }
            else
            {
                var appDataPath = environmentService.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                databasePath = fileSystem.Path.Combine(appDataPath, ApplicationName, $"{ApplicationName}.db");
            }

            var dbContextOptionsBuilder = new DbContextOptionsBuilder<DataContext>();
            dbContextOptionsBuilder.UseSqlite($"Data Source={databasePath}");

            return dbContextOptionsBuilder.Options;
        }
    }
}
