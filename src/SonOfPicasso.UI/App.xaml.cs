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
using SonOfPicasso.Core.Scheduling;
using SonOfPicasso.Core.Services;
using SonOfPicasso.Data.Context;
using SonOfPicasso.Data.Repository;
using SonOfPicasso.UI.Injection;
using SonOfPicasso.UI.Interfaces;
using SonOfPicasso.UI.Scheduling;
using SonOfPicasso.UI.Services;
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
            containerBuilder.RegisterType<ApplicationViewModel>()
                .As<IApplicationViewModel>()
                .InstancePerLifetimeScope();

            containerBuilder.RegisterType<MainWindow>()
                .InstancePerLifetimeScope();

            containerBuilder.RegisterType<EnvironmentService>()
                .As<IEnvironmentService>()
                .InstancePerLifetimeScope();

            containerBuilder.RegisterType<FileSystem>()
                .As<IFileSystem>()
                .InstancePerLifetimeScope();

            containerBuilder.RegisterType<SchedulerProvider>()
                .As<ISchedulerProvider>()
                .InstancePerLifetimeScope();

            containerBuilder.RegisterType<ImageManagementService>()
                .As<IImageManagementService>()
                .InstancePerLifetimeScope();

            containerBuilder.RegisterType<ImageLocationService>()
                .As<IImageLocationService>()
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

            containerBuilder.RegisterType<DataCache>()
                .As<IDataCache>();

            containerBuilder.RegisterType<UnitOfWork>()
                .As<IUnitOfWork>();

            containerBuilder.RegisterType<ImageViewModel>()
                .As<IImageViewModel>();

            containerBuilder.RegisterType<ExifDataService>()
                .As<IExifDataService>();

            containerBuilder.RegisterType<ImageFolderViewModel>()
                .As<IImageFolderViewModel>();

            containerBuilder.RegisterType<ImageLoadingService>()
                .As<IImageLoadingService>();

            containerBuilder.RegisterType<ImageViewControl>()
                .AsSelf();

            containerBuilder.RegisterType<ImageFolderViewControl>()
                .AsSelf();

            containerBuilder.RegisterLogger();
            var container = containerBuilder.Build();
            var resolver = new AutofacDependencyResolver(container);

            Locator.SetLocator(resolver);
            Locator.CurrentMutable.InitializeReactiveUI();

            var updatedBuilder = new ContainerBuilder();
            
            updatedBuilder.RegisterType<CustomViewLocator>()
                .As<IViewLocator>();

            resolver.UpdateComponentContext(updatedBuilder);

            Locator.CurrentMutable.RegisterPlatformBitmapLoader();
            Locator.CurrentMutable.UseSerilogFullLogger();

            SQLitePCL.Batteries_V2.Init();

            var dataContext = container.Resolve<DataContext>();
            dataContext.Database.Migrate();

            var mainWindow = container.Resolve<MainWindow>();

            mainWindow.ViewModel = container.Resolve<IApplicationViewModel>();
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
