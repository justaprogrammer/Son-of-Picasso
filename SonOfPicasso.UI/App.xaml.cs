using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO.Abstractions;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using SonOfPicasso.Core.Interfaces;
using SonOfPicasso.Core.Logging;
using SonOfPicasso.Core.Scheduling;
using SonOfPicasso.Core.Services;
using SonOfPicasso.UI.Interfaces;
using SonOfPicasso.UI.Scheduling;
using SonOfPicasso.UI.ViewModels;
using SonOfPicasso.UI.Views;

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

            var serviceCollection = new ServiceCollection()
                .AddLogging(builder => builder.AddSerilog())
                .AddSingleton<IFileSystem, FileSystem>()
                .AddSingleton<ISchedulerProvider, SchedulerProvider>()
                .AddSingleton<IImageLoadingService, ImageLoadingService>()
                .AddSingleton<IImageLocationService, ImageLocationService>()
                .AddSingleton<ISharedCache, SharedCache>()
                .AddSingleton<IEnvironmentService, EnvironmentService>()
                .AddScoped<IApplicationViewModel, ApplicationViewModel>()
                .AddScoped<IImageFolderViewModel, ImageFolderViewModel>()
                .Add<IImageViewModel, ImageViewModel>()
                .AddScoped<MainWindow>();

            var serviceProvider = serviceCollection.BuildServiceProvider();

            var mainWindow = serviceProvider.GetService<MainWindow>();
            mainWindow.ViewModel = serviceProvider.GetService<IApplicationViewModel>();
            mainWindow.Show();
            mainWindow.ViewModel.Initialize().Subscribe();
        }
    }
}
