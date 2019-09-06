using System;
using System.IO.Abstractions;
using System.Reactive.Linq;
using FluentColorConsole;
using McMaster.Extensions.CommandLineUtils;
using Serilog;
using SonOfPicasso.Core.Interfaces;
using SonOfPicasso.Core.Logging;
using SonOfPicasso.Core.Scheduling;
using SonOfPicasso.Core.Services;
using SonOfPicasso.Tools.Extensions;
using SonOfPicasso.Tools.Services;

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
                    var path = setCmd.Argument<string>("path", "The location for these images").IsRequired();

                    setCmd.OnExecute(() => ToolsService.GenerateImages(count.ParsedValue, path.ParsedValue).LastAsync().Wait());
                });
            });

            app.Command("cache", configCmd =>
            {
                configCmd.HandleSpecifySubCommandError();

                configCmd.Command("clear", setCmd =>
                {
                    setCmd.Description = "Clear Cache";

                    setCmd.HandleValidationError();

                    setCmd.OnExecute(() => ToolsService.ClearCache().LastAsync().Wait());
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

                    throw new NotImplementedException();

//                    var serviceCollection = new ServiceCollection()
//                        .AddLogging(builder => builder.AddSerilog())
//                        .AddSingleton<IFileSystem, FileSystem>()
//                        .AddSingleton<ISchedulerProvider, ConsoleSchedulerProvider>()
//                        .AddSingleton<IImageLocationService, ImageLocationService>()
//                        .AddSingleton<IDataCache, DataCache>()
//                        .AddSingleton<IEnvironmentService, EnvironmentService>()
//                        .AddSingleton<ToolsService>();

//                    var serviceProvider = serviceCollection.BuildServiceProvider();
//                    _toolsService = serviceProvider.GetService<ToolsService>();
                }

                return _toolsService;
            }
        }
    }
}