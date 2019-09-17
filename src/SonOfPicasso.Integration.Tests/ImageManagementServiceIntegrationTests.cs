using System;
using System.IO.Abstractions;
using System.Reactive.Linq;
using Autofac;
using AutofacSerilogIntegration;
using Microsoft.EntityFrameworkCore;
using SonOfPicasso.Core.Interfaces;
using SonOfPicasso.Core.Scheduling;
using SonOfPicasso.Core.Services;
using SonOfPicasso.Data.Context;
using SonOfPicasso.Data.Repository;
using SonOfPicasso.Tools.Services;
using SonOfPicasso.UI.Scheduling;
using Xunit;
using Xunit.Abstractions;

namespace SonOfPicasso.Integration.Tests
{
    public class ImageManagementServiceIntegrationTests : IntegrationTestsBase, IDisposable
    {
        public ImageManagementServiceIntegrationTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterLogger();
            containerBuilder.RegisterType<ImageManagementService>().AsSelf();
            containerBuilder.RegisterType<FileSystem>().As<IFileSystem>();
            containerBuilder.RegisterType<ImageLocationService>().As<IImageLocationService>();
            containerBuilder.RegisterInstance(DbContextOptions).As<DbContextOptions<DataContext>>();
            containerBuilder.RegisterType<UnitOfWork>().As<IUnitOfWork>();
            containerBuilder.RegisterType<SchedulerProvider>().As<ISchedulerProvider>();
            containerBuilder.RegisterType<ExifDataService>().As<IExifDataService>();
            containerBuilder.RegisterType<ImageGenerationService>().AsSelf();
            _container = containerBuilder.Build();

            _imagesPath = FileSystem.Path.Combine(TestRoot, "Images");
            FileSystem.Directory.CreateDirectory(_imagesPath);

            var imageGenerationService = _container.Resolve<ImageGenerationService>();
            imageGenerationService.GenerateImages(2, _imagesPath).Wait();
        }

        public override void Dispose()
        {
            base.Dispose();

            _container.Dispose();
        }

        private readonly IContainer _container;
        private readonly string _imagesPath;

        [Fact]
        public void ShouldScanFolder()
        {
            var imageManagementService = _container.Resolve<ImageManagementService>();
            imageManagementService.ScanFolder(_imagesPath).Wait();
        }
    }
}