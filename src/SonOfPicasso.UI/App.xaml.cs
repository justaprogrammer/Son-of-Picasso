using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Windows;
using Autofac;
using Autofac.Builder;
using AutofacSerilogIntegration;
using ReactiveUI;
using Serilog;
using SonOfPicasso.Core.Interfaces;
using SonOfPicasso.Core.Logging;
using SonOfPicasso.Core.Scheduling;
using SonOfPicasso.Core.Services;
using SonOfPicasso.UI.Injection;
using SonOfPicasso.UI.Interfaces;
using SonOfPicasso.UI.Scheduling;
using SonOfPicasso.UI.Services;
using SonOfPicasso.UI.ViewModels;
using SonOfPicasso.UI.Views;
using SonOfPicasso.UI.Windows;
using Splat;
using Splat.Autofac;
using Splat.Serilog;

namespace SonOfPicasso.UI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
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

            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterType<ApplicationViewModel>().As<IApplicationViewModel>().InstancePerLifetimeScope();
            containerBuilder.RegisterType<MainWindow>().InstancePerLifetimeScope();
            
//            containerBuilder.RegisterType<NullLoggerFactory>().As<ILoggerFactory>().SingleInstance();
//            containerBuilder.RegisterType<SerilogLoggerProvider>().As<ILoggerProvider>().SingleInstance();

            containerBuilder.RegisterLogger();

            var container = containerBuilder.Build();
            var resolve = container.Resolve<MainWindow>();

//            containerBuilder.UseAutofacDependencyResolver();
//            Locator.CurrentMutable.UseMicrosoftExtensionsLoggingWithWrappingFullLogger(new LoggerFactory(new ILoggerProvider[]{new MicrosoftExtensionsLogProvider()}));
            
            //        .AddSingleton<IFileSystem, FileSystem>()
            //            .AddSingleton<ISchedulerProvider, SchedulerProvider>()
            //            .AddSingleton<IImageLoadingService, ImageLoadingService>()
            //            .AddSingleton<IImageLocationService, ImageLocationService>()
            //            .AddSingleton<IImageManagementService, ImageManagementService>()
            //            .AddSingleton<IDataCache, DataCache>()
            //            .AddSingleton<IEnvironmentService, EnvironmentService>()
            //            .AddTransient<IApplicationViewModel, ApplicationViewModel>()
            //            .AddTransient<IImageFolderViewModel, ImageFolderViewModel>()
            //            .AddTransient<IImageViewModel, ImageViewModel>()
            //            .AddTransient<ImageFolderViewControl>()
            //            .AddTransient<ImageViewControl>()
            //            .AddTransient<MainWindow>()
            //            .AddTransient<IViewLocator, CustomViewLocator>();

//            var mainWindow = Locator.Current.GetService<MainWindow>();
//            mainWindow.ViewModel = Locator.Current.GetService<IApplicationViewModel>();
//            mainWindow.Show();
//            mainWindow.ViewModel.Initialize().Subscribe();

            //            var serviceCollection = new ServiceCollection()
//                            .AddLogging(builder => builder.AddSerilog())
            //                .AddSingleton<IFileSystem, FileSystem>()
            //                .AddSingleton<ISchedulerProvider, SchedulerProvider>()
            //                .AddSingleton<IImageLoadingService, ImageLoadingService>()
            //                .AddSingleton<IImageLocationService, ImageLocationService>()
            //                .AddSingleton<IImageManagementService, ImageManagementService>()
            //                .AddSingleton<IDataCache, DataCache>()
            //                .AddSingleton<IEnvironmentService, EnvironmentService>()
            //                .AddTransient<IApplicationViewModel, ApplicationViewModel>()
            //                .AddTransient<IImageFolderViewModel, ImageFolderViewModel>()
            //                .AddTransient<IImageViewModel, ImageViewModel>()
            //                .AddTransient<ImageFolderViewControl>()
            //                .AddTransient<ImageViewControl>()
            //                .AddTransient<MainWindow>()
            //                .AddTransient<IViewLocator, CustomViewLocator>();

            //            var serviceProvider = serviceCollection.BuildServiceProvider();
            //
            //            using (Locator.SuppressResolverCallbackChangedNotifications())
            //            {
            //                Locator.SetLocator(new ServiceCollectionDependencyResolver(serviceCollection));
            //                Locator.CurrentMutable.UseSerilogFullLogger(Log.Logger);    
            //            }

            //            var mainWindow = serviceProvider.GetService<MainWindow>();
            //            mainWindow.ViewModel = serviceProvider.GetService<IApplicationViewModel>();
            //            mainWindow.Show();
            //            mainWindow.ViewModel.Initialize().Subscribe();
        }
    }
}
