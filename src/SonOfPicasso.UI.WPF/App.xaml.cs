using System;
using System.IO.Abstractions;
using System.Linq;
using System.Windows;
using Akavache;
using Autofac;
using AutofacSerilogIntegration;
using Microsoft.EntityFrameworkCore;
using ReactiveUI;
using Serilog;
using Serilog.Events;
using Serilog.Filters;
using SonOfPicasso.Core;
using SonOfPicasso.Core.Interfaces;
using SonOfPicasso.Core.Logging;
using SonOfPicasso.Core.Scheduling;
using SonOfPicasso.Core.Services;
using SonOfPicasso.Data.Repository;
using SonOfPicasso.Data.Services;
using SonOfPicasso.UI.WPF.ViewModels;
using SonOfPicasso.UI.WPF.Windows;
using Splat;
using Splat.Serilog;
using SQLitePCL;
using ILogger = Serilog.ILogger;

namespace SonOfPicasso.UI.WPF
{
    /// <summary>
    ///     Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private const string ApplicationName = "SonOfPicasso";

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var loggerConfiguration = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .Enrich.WithThreadId()
                .Enrich.With<CustomEnrichers>();

            var outputTemplate =
                "{Timestamp:HH:mm:ss} [{Level:u4}] ({PaddedThreadId}) {ShortSourceContext} {Message}{NewLineIfException}{Exception}{NewLine}";

            if (Common.IsDebug)
                loggerConfiguration = loggerConfiguration
                    .WriteTo.Debug(outputTemplate: outputTemplate);
            else if (Common.IsTrace)
                loggerConfiguration = loggerConfiguration
                    .WriteTo.Trace(outputTemplate: outputTemplate);

            if (Common.IsVerboseLoggingEnabled) loggerConfiguration = loggerConfiguration.MinimumLevel.Verbose();

            Func<LogEvent, bool>[] matches =
            {
                Matching.FromSource<ImageLoadingService>()
            };

            loggerConfiguration = loggerConfiguration.WriteTo.Logger(configuration =>
            {
                configuration
                    .Filter.ByExcluding(logEvent =>
                        matches.Select(func => func(logEvent)).Any() && logEvent.Level <= LogEventLevel.Verbose)
                    .WriteTo
                    .File("SonOfPicasso.log", outputTemplate: outputTemplate);
            });

            Log.Logger = loggerConfiguration.CreateLogger();

            BlobCache.ApplicationName = ApplicationName;
            BlobCache.EnsureInitialized();

            var containerBuilder = new ContainerBuilder();

            containerBuilder.RegisterType<FileSystem>()
                .As<IFileSystem>()
                .InstancePerLifetimeScope();

            containerBuilder.Register(context => context.Resolve<IFileSystem>().DriveInfo)
                .As<IDriveInfoFactory>()
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

            containerBuilder.Register<IImageLoadingService>(context =>
                {
                    var fileSystem = context.Resolve<IFileSystem>();
                    var environmentService = context.Resolve<IEnvironmentService>();
                    string cacheFolderOverride = null;

                    var cachePath = environmentService.GetEnvironmentVariable("SonOfPicasso_CachePath");
                    if (!string.IsNullOrWhiteSpace(cachePath))
                    {
                        var directoryInfo = fileSystem.DirectoryInfo.FromDirectoryName(cachePath);
                        directoryInfo.Create();

                        cacheFolderOverride = directoryInfo.CreateSubdirectory("Thumbnails").FullName;
                    }

                    return new ImageLoadingService(fileSystem, 
                        context.Resolve<ILogger>().ForContext<ImageLoadingService>(),
                        context.Resolve<ISchedulerProvider>(), 
                        context.Resolve<IBlobCacheProvider>(), 
                        cacheFolderOverride);

                }).As<IImageLoadingService>()
                .InstancePerLifetimeScope();

            containerBuilder.Register<IBlobCacheProvider>(context =>
                {
                    var environmentService = context.Resolve<IEnvironmentService>();
                    var cachePath = environmentService.GetEnvironmentVariable("SonOfPicasso_CachePath");
                    
                    if (!string.IsNullOrWhiteSpace(cachePath))
                    {
                        var fileSystem = context.Resolve<IFileSystem>();

                        var directoryInfo = fileSystem.DirectoryInfo.FromDirectoryName(cachePath);
                        directoryInfo.Create();

                        var blobCacheProvider = new CustomBlobCacheProvider(fileSystem, cachePath);
                        return blobCacheProvider;
                    }
                    
                    return new BlobCacheProvider();

                }).As<IBlobCacheProvider>()
                .InstancePerLifetimeScope();

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

            Batteries_V2.Init();

            var dataContext = container.Resolve<DataContext>();
            dataContext.Database.Migrate();

            var mainWindow = container.Resolve<MainWindow>();

            mainWindow.ViewModel = container.Resolve<ApplicationViewModel>();
            mainWindow.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            BlobCache.Shutdown().Wait();

            base.OnExit(e);
        }

        internal static DbContextOptions<DataContext> BuildDbContextOptions(IEnvironmentService environmentService,
            IFileSystem fileSystem)
        {
            var databasePath = environmentService.GetEnvironmentVariable("SonOfPicasso_DatabasePath");
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