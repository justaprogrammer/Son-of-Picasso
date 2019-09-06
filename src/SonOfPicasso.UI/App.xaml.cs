﻿using System;
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

            Akavache.Sqlite3.Registrations.Start("SonOfPicasso", () => SQLitePCL.Batteries_V2.Init());

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

            containerBuilder.RegisterType<DataCache>()
                .As<IDataCache>();

            containerBuilder.RegisterType<ImageViewModel>()
                .As<IImageViewModel>();

            containerBuilder.RegisterType<ImageFolderViewModel>()
                .As<IImageFolderViewModel>();

            containerBuilder.RegisterLogger();

            var container = containerBuilder.Build();
            var mainWindow = container.Resolve<MainWindow>();

            mainWindow.ViewModel = container.Resolve<IApplicationViewModel>();
            mainWindow.Show();
            mainWindow.ViewModel.Initialize().Subscribe();
        }
    }
}
