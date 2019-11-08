using System;
using System.IO.Abstractions;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Autofac;
using AutofacSerilogIntegration;
using Dapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using SonOfPicasso.Core.Interfaces;
using SonOfPicasso.Core.Model;
using SonOfPicasso.Core.Scheduling;
using SonOfPicasso.Core.Services;
using SonOfPicasso.Data.Interfaces;
using SonOfPicasso.Data.Model;
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

        }

        private void GenerateImages(int count)
        {
            _imageCount = count;

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
        private int _directoryCount;
        private int _imageCount;

        [Fact]
        public async Task ShouldScan()
        {
            GenerateImages(50);

            var imageManagementService = _container.Resolve<ImageManagementService>();
            var imageContainers = await imageManagementService
                .ScanFolder(_imagesPath)
                .ToArray();

            imageContainers.Should().HaveCount(_directoryCount);

            var images = (await Connection.QueryAsync<Image>("SELECT * FROM Images"))
                .ToArray();

            images.Should().HaveCount(_imageCount);

            var folders = (await Connection.QueryAsync<Folder>("SELECT * FROM Folders"))
                .ToArray();

            folders.Should().HaveCount(_directoryCount);

            var containers = await imageManagementService
                .GetAllImageContainers()
                .ToArray();

            containers.Length
                .Should()
                .Be(_directoryCount);
        }


        [Fact]
        public async Task ShouldCreateAlbum()
        {
            var imageCount = 10;
            var albumImageCount = 5;
            
            GenerateImages(imageCount);

            var imageManagementService = _container.Resolve<ImageManagementService>();
            var imageContainers = await imageManagementService
                .ScanFolder(_imagesPath)
                .ToArray();

            var imageRefs = Faker.PickRandom(imageContainers.SelectMany(container => container.ImageRefs.Select(imageRef => imageRef)), albumImageCount)
                .ToArray();

            var imageIds = imageRefs.Select(imageRef => imageRef.ImageId)
                .ToArray();

            ICreateAlbum createAlbum = new TestCreateAlbum
            {
                AlbumName = Faker.Random.Word(),
                AlbumDate = imageRefs
                    .Select(imageRef => imageRef.ContainerDate)
                    .Min()
                    .Date
            };

            var imageContainer = await imageManagementService.CreateAlbum(createAlbum);
            imageContainer.Name.Should().Be(createAlbum.AlbumName);
            imageContainer.Date.Should().Be(createAlbum.AlbumDate);
            imageContainer.ContainerType.Should().Be(ImageContainerTypeEnum.Album);
            imageContainer.ImageRefs.Should().BeEmpty();
            imageContainer.Year.Should().Be(createAlbum.AlbumDate.Year);

            imageContainer = await imageManagementService.AddImagesToAlbum(imageContainer.ContainerTypeId, imageIds);
            imageContainer.Name.Should().Be(createAlbum.AlbumName);
            imageContainer.Date.Should().Be(createAlbum.AlbumDate);
            imageContainer.ContainerType.Should().Be(ImageContainerTypeEnum.Album);
            imageContainer.ImageRefs.Should().HaveCount(albumImageCount);
            imageContainer.Year.Should().Be(createAlbum.AlbumDate.Year);

            var albums = (await Connection.QueryAsync<Album>("SELECT * FROM Albums"))
                .ToArray();

            albums.Should().ContainSingle();

            var albumImages = (await Connection.QueryAsync<AlbumImage>("SELECT * FROM AlbumImages"))
                .ToArray();

            albumImages.Should().HaveCount(5);
        }

        class TestCreateAlbum : ICreateAlbum
        {
            public string AlbumName { get; set; }

            public DateTime AlbumDate { get; set; }
        }
    }
}