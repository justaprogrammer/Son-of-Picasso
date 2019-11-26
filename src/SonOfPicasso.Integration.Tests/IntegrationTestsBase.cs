using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Autofac;
using AutofacSerilogIntegration;
using Microsoft.EntityFrameworkCore;
using Serilog;
using SonOfPicasso.Core.Scheduling;
using SonOfPicasso.Data.Interfaces;
using SonOfPicasso.Data.Repository;
using SonOfPicasso.Data.Services;
using SonOfPicasso.Testing.Common;
using SonOfPicasso.Tools.Services;
using SonOfPicasso.UI.WPF.Services;
using Xunit.Abstractions;

namespace SonOfPicasso.Integration.Tests
{
    public abstract class IntegrationTestsBase : TestsBase
    {
        protected readonly FileSystem FileSystem = new FileSystem();
        protected string DatabasePath;
        protected DataContext DataContext;
        protected DbContextOptions<DataContext> DbContextOptions;
        protected string ImagesPath;
        protected string TestPath;

        protected IntegrationTestsBase(LoggerConfiguration loggerConfiguration)
            : base(loggerConfiguration)
        {
            Initialize();
        }

        protected IntegrationTestsBase(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
            Initialize();
        }

        protected abstract IContainer Container { get; }
        protected ImageGenerationService ImageGenerationService => Container.Resolve<ImageGenerationService>();
        protected IDirectoryInfo ImagesDirectoryInfo => FileSystem.DirectoryInfo.FromDirectoryName(ImagesPath);
        protected ISchedulerProvider SchedulerProvider => Container.Resolve<ISchedulerProvider>();

        private void Initialize()
        {
            TestPath = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), "SonOfPicasso.IntegrationTests",
                Guid.NewGuid().ToString());
            FileSystem.Directory.CreateDirectory(TestPath);

            ImagesPath = FileSystem.Path.Combine(TestPath, "Images");
            FileSystem.Directory.CreateDirectory(ImagesPath);

            DatabasePath = FileSystem.Path.Combine(TestPath, "database.db");

            DbContextOptions =
                new DbContextOptionsBuilder<DataContext>()
                    .UseSqlite($"Data Source={DatabasePath}")
                    .Options;
        }

        protected ContainerBuilder GetContainerBuilder()
        {
            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterLogger();
            containerBuilder.RegisterType<FileSystem>().As<IFileSystem>();
            containerBuilder.RegisterInstance(DbContextOptions).As<DbContextOptions<DataContext>>();
            containerBuilder.RegisterType<UnitOfWork>()
                .As<IUnitOfWork>()
                .AsSelf();
            containerBuilder.RegisterType<SchedulerProvider>().As<ISchedulerProvider>();
            containerBuilder.RegisterType<ImageGenerationService>().AsSelf();
            return containerBuilder;
        }

        public override void Dispose()
        {
            base.Dispose();

            Container?.Dispose();
            DataContext?.Dispose();

            if (FileSystem.Directory.Exists(TestPath))
                try
                {
                    FileSystem.Directory.Delete(TestPath, true);
                }
                catch (Exception e)
                {
                    Logger.Warning(e, "Unable to delete test directory {TestPath}", TestPath);

                    foreach (var file in FileSystem.Directory.EnumerateFiles(TestPath, "*.*",
                        SearchOption.AllDirectories))
                        try
                        {
                            FileSystem.File.Delete(file);
                        }
                        catch (Exception e1)
                        {
                            Logger.Error(e1, "Unable to delete file {File}", file);
                        }
                }
        }

        protected async Task InitializeDataContextAsync()
        {
            DataContext = new DataContext(DbContextOptions);
            await DataContext.Database.MigrateAsync();
        }

        protected async Task<IDictionary<string, string[]>> GenerateImagesAsync(int count, string imagesPath = null)
        {
            return await ImageGenerationService.GenerateImages(count, imagesPath ?? ImagesPath, imagesPath == null)
                .SelectMany(observable => observable.ToArray().Select(items => (observable.Key, items)))
                .ToDictionary(tuple => tuple.Key, tuple => tuple.items);
        }
    }
}