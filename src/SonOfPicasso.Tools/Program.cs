using System;
using System.IO.Abstractions;
using FluentColorConsole;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using SonOfPicasso.Core.Interfaces;
using SonOfPicasso.Core.Logging;
using SonOfPicasso.Core.Scheduling;
using SonOfPicasso.Core.Services;

namespace SonOfPicasso.Tools
{
    class Program
    {
        public static int Main(string[] args)
        {
            var app = new CommandLineApplication
            {
                Name = "SonOfPicasso Tools",
                Description = "Utility to help develop on SonOfPicasso",
            };

            app.HelpOption(inherited: true);
            app.Command("images", configCmd =>
            {
                configCmd.HandleSpecifySubCommandError();

                configCmd.Command("generate", setCmd =>
                {
                    setCmd.Description = "Generate Images";

                    setCmd.HandleValidationError();

                    var count = setCmd.Argument<int>("count", "The number of images to generate").IsRequired();
                    setCmd.OnExecute(() => ToolsService.GenerateImages(count.ParsedValue));
                });
            });

            app.HandleSpecifySubCommandError();

            return app.Execute(args);
        }

        private static ToolsService _toolsService;

        private static ToolsService ToolsService
        {
            get
            {
                if(_toolsService == null)
                {
                    var loggerConfiguration = new LoggerConfiguration()
                        .MinimumLevel.Verbose()
                        .Enrich.WithThreadId()
                        .Enrich.With<CustomEnrichers>();

                    loggerConfiguration
                        .WriteTo.Debug(
                            outputTemplate:
                            "{Timestamp:HH:mm:ss} [{Level:u4}] ({PaddedThreadId}) {ShortSourceContext} {Message}{NewLineIfException}{Exception}{NewLine}");

                    Log.Logger = loggerConfiguration.CreateLogger();

                    var serviceCollection = new ServiceCollection()
                        .AddLogging(builder => builder.AddSerilog())
                        .AddSingleton<IFileSystem, FileSystem>()
                        .AddSingleton<ISchedulerProvider, ConsoleSchedulerProvider>()
                        .AddSingleton<IImageLoadingService, ImageLoadingService>()
                        .AddSingleton<IImageLocationService, ImageLocationService>()
                        .AddSingleton<ISharedCache, SharedCache>()
                        .AddSingleton<IEnvironmentService, EnvironmentService>()
                        .AddSingleton<ToolsService>();

                    var serviceProvider = serviceCollection.BuildServiceProvider();
                    _toolsService = serviceProvider.GetService<ToolsService>();
                }

                return _toolsService;
            }
        }
    }
}