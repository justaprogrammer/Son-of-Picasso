using System;
using System.IO.Abstractions;
using System.Linq;
using System.Reactive.Linq;
using Autofac;
using AutofacSerilogIntegration;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SonOfPicasso.Core.Interfaces;
using SonOfPicasso.Core.Scheduling;
using SonOfPicasso.Core.Services;
using SonOfPicasso.Data.Interfaces;
using SonOfPicasso.Data.Repository;
using SonOfPicasso.Data.Services;
using SonOfPicasso.Tools.Services;
using SonOfPicasso.UI.Services;
using Xunit;
using Xunit.Abstractions;

namespace SonOfPicasso.Integration.Tests.Services
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
            containerBuilder.RegisterType<UnitOfWork>()
                .As<IUnitOfWork>()
                .AsSelf();
            containerBuilder.RegisterType<SchedulerProvider>().As<ISchedulerProvider>();
            containerBuilder.RegisterType<ExifDataService>().As<IExifDataService>();
            containerBuilder.RegisterType<ImageGenerationService>().AsSelf();
            _container = containerBuilder.Build();

            _imagesPath = FileSystem.Path.Combine(TestPath, "Images");
            FileSystem.Directory.CreateDirectory(_imagesPath);

            _imageCount = Faker.Random.Int(50, 75);
            _imageCount = 1;

            var imageGenerationService = _container.Resolve<ImageGenerationService>();
            var groupedObservable = imageGenerationService.GenerateImages(_imageCount, _imagesPath)
                .ToArray()
                .Wait();

            _directoryCount = groupedObservable.Length;
        }

        public override void Dispose()
        {
            base.Dispose();

            _container.Dispose();
        }

        private readonly IContainer _container;
        private readonly string _imagesPath;
        private readonly int _directoryCount;
        private readonly int _imageCount;

        [Fact]
        public void ShouldScanAndGetAllImageContainers()
        {
            var imageManagementService = _container.Resolve<ImageManagementService>();
            var imageContainers = imageManagementService.ScanFolder(_imagesPath)
                .ToArray()
                .Wait();

            imageContainers.Length.Should().Be(_directoryCount);

            var unitOfWorkFactory = _container.Resolve<Func<UnitOfWork>>();
            using (var unitOfWork = unitOfWorkFactory())
            {
                var i = unitOfWork.ImageRepository.Get().ToArray();
                i.Length.Should().Be(_imageCount);

                var d = unitOfWork.FolderRepository.Get().ToArray();
                d.Length.Should().Be(_directoryCount);
            }

            var containers = imageManagementService.GetAllImageContainers()
                .ToArray()
                .Wait();

            containers.Length
                .Should()
                .Be(_directoryCount);
        }
    }
}