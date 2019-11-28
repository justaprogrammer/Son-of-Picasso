using System;
using System.IO.Abstractions;
using System.Linq;
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
using Splat;
using Splat.Serilog;
using SQLitePCL;
using ILogger = Serilog.ILogger;

namespace SonOfPicasso.UI
{
    public class AppConfiguration
    {
        public static ContainerBuilder Configure(string applicationName)
        {
            ConfigureLogging();
            ConfigureAkavache(applicationName);
            ConfigureSqlite();

            return ConfigureContainerBuilder(applicationName);
        }

        private static void ConfigureSqlite()
        {
            Batteries_V2.Init();
        }

        public static void ConfigureContainer(IContainer container)
        {
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
        }

        private static ContainerBuilder ConfigureContainerBuilder(string applicationName)
        {
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

                    return BuildDbContextOptions(environmentService, fileSystem, applicationName);
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

            containerBuilder.RegisterAssemblyTypes(typeof(AppConfiguration).Assembly)
                .Where(type => type.Namespace.StartsWith("SonOfPicasso.UI.Services"))
                .InstancePerLifetimeScope()
                .AsImplementedInterfaces();

            containerBuilder.RegisterAssemblyTypes(typeof(AppConfiguration).Assembly)
                .Where(type => type.Namespace.StartsWith("SonOfPicasso.UI.ViewModels"))
                .AsImplementedInterfaces()
                .AsSelf();

            containerBuilder.RegisterLogger();
            return containerBuilder;
        }

        private static void ConfigureAkavache(string applicationName)
        {
            BlobCache.ApplicationName = applicationName;
            BlobCache.EnsureInitialized();
        }

        private static void ConfigureLogging()
        {
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
        }

        internal static DbContextOptions<DataContext> BuildDbContextOptions(IEnvironmentService environmentService,
            IFileSystem fileSystem, string applicationName)
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
                databasePath = fileSystem.Path.Combine(appDataPath, applicationName, $"{applicationName}.db");
            }

            var dbContextOptionsBuilder = new DbContextOptionsBuilder<DataContext>();
            dbContextOptionsBuilder.UseSqlite($"Data Source={databasePath}");

            return dbContextOptionsBuilder.Options;
        }
    }
}