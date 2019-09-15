using System;
using System.IO.Abstractions;
using System.Reactive.Linq;
using Autofac;
using FluentColorConsole;
using McMaster.Extensions.CommandLineUtils;
using Serilog;
using SonOfPicasso.Core.Interfaces;
using SonOfPicasso.Core.Logging;
using SonOfPicasso.Core.Scheduling;
using SonOfPicasso.Core.Services;
using SonOfPicasso.Testing.Common.Services;
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

                    setCmd.OnExecute(() => ImageGenerationService.GenerateImages(count.ParsedValue, path.ParsedValue).LastAsync().Wait());
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

        private static IContainer _container;

        private static IContainer Container
        {
            get
            {
                if (_container == null)
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

                    var containerBuilder = new ContainerBuilder();

                    containerBuilder.RegisterType<EnvironmentService>()
                        .As<IEnvironmentService>()
                        .InstancePerLifetimeScope();

                    containerBuilder.RegisterType<FileSystem>()
                        .As<IFileSystem>()
                        .InstancePerLifetimeScope();

                    containerBuilder.RegisterType<ConsoleSchedulerProvider>()
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

                    containerBuilder.RegisterType<ToolsService>();

                    _container = containerBuilder.Build();
                }

                return _container;
            }
        }

        private static ToolsService _toolsService;

        private static ToolsService ToolsService
        {
            get
            {
                if(_toolsService == null)
                {
                    _toolsService = Container.Resolve<ToolsService>();
                }

                return _toolsService;
            }
        }

        private static ImageGenerationService _imageGenerationService;

        private static ImageGenerationService ImageGenerationService
        {
            get
            {
                if(_imageGenerationService == null)
                {
                    _imageGenerationService = Container.Resolve<ImageGenerationService>();
                }

                return _imageGenerationService;
            }
        }
    }
}