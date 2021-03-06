﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO.Abstractions;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Autofac;
using Dapper;
using DynamicData;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using ReactiveUI;
using Serilog;
using Serilog.Filters;
using SonOfPicasso.Core.Interfaces;
using SonOfPicasso.Core.Model;
using SonOfPicasso.Core.Scheduling;
using SonOfPicasso.Core.Services;
using SonOfPicasso.Data.Model;
using SonOfPicasso.Testing.Common;
using SonOfPicasso.Testing.Common.Scheduling;
using Xunit;
using Xunit.Abstractions;

namespace SonOfPicasso.Integration.Tests.Services
{
    public class ImageContainerOperationServiceIntegrationTests : IntegrationTestsBase
    {
        public ImageContainerOperationServiceIntegrationTests(ITestOutputHelper testOutputHelper)
            : base(GetCustomConfiguration(testOutputHelper))
        {
            var containerBuilder = GetContainerBuilder();
            containerBuilder.RegisterType<ImageContainerOperationService>();
            containerBuilder.RegisterType<ExifDataService>().As<IExifDataService>();
            containerBuilder.RegisterType<TestBlobCacheProvider>().As<IBlobCacheProvider>();

            containerBuilder.Register(context =>
            {
                var resolve = context.Resolve<ILogger>();
                var directoryInfo = ImagesDirectoryInfo.Parent.CreateSubdirectory("Thumbnails");

                return new ImageLoadingService(context.Resolve<IFileSystem>(), resolve,
                    context.Resolve<ISchedulerProvider>(), context.Resolve<IBlobCacheProvider>(),
                    directoryInfo.FullName);
            }).As<IImageLoadingService>();

            containerBuilder.RegisterType<ImageLocationService>().As<IImageLocationService>();
            Container = containerBuilder.Build();
        }

        private static LoggerConfiguration GetCustomConfiguration(ITestOutputHelper testOutputHelper) =>
            GetLoggerConfiguration(testOutputHelper, configuration => configuration.Filter.ByExcluding(Matching.FromSource<ExifDataService>()));

        protected override IContainer Container { get; }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                Container.Dispose();
            }
        }

        private class TestCreateAlbum : ICreateAlbum
        {
            public string AlbumName { get; set; }

            public DateTime AlbumDate { get; set; }
        }

        [Fact]
        public async Task ShouldScanAndDeleteImage()
        {
            await InitializeDataContextAsync();
            await using var connection = DataContext.Database.GetDbConnection();
            var imagesCount = 5;
            var generatedImages = await GenerateImagesAsync(imagesCount);

            var imagesDirectoryInfo = FileSystem.DirectoryInfo.FromDirectoryName(ImagesPath);
            var directoryCount = imagesDirectoryInfo.EnumerateDirectories().Count();

            var imageContainerOperationService = Container.Resolve<ImageContainerOperationService>();
            var scannedImageRefs = new ObservableCollection<ImageRef>();

            imageContainerOperationService.ScanImageObservable
                .Subscribe(imageRef =>
                {
                    scannedImageRefs.Add(imageRef);
                });

            await imageContainerOperationService
                .ScanFolder(ImagesPath, new SourceCache<ImageRef, string>(imageRef => imageRef.ImagePath));

            scannedImageRefs
                .WhenAny(refs => refs.Count, change => change.Value)
                .Subscribe(i =>
                {
                    if (i == imagesCount)
                    {
                        Set();
                    }
                });

            WaitOne(45);

            scannedImageRefs.Should().HaveCount(imagesCount);

            var images = (await connection.QueryAsync<Image>("SELECT * FROM Images"))
                .ToArray();

            images.Should().HaveCount(imagesCount);

            var folders = (await connection.QueryAsync<Folder>("SELECT * FROM Folders"))
                .ToArray();

            folders.Should().HaveCount(directoryCount);

            var exifDatas = (await connection.QueryAsync("SELECT * FROM ExifData"))
                .ToArray();

            exifDatas.Should().HaveCount(imagesCount);

            var containers = await imageContainerOperationService
                .GetAllImageContainers()
                .ToArray();

            containers.Length
                .Should()
                .Be(directoryCount);

            containers.SelectMany(container => container.ImageRefs)
                .Should().HaveCount(imagesCount);

            var imageToDelete = Faker.PickRandom(images);
            FileSystem.File.Delete(imageToDelete.Path);

            var deletedImageContainer = await imageContainerOperationService.DeleteImage(imageToDelete.Path);
            deletedImageContainer.Id.Should().Be(imageToDelete.FolderId);

            imagesCount -= 1;

            images = (await connection.QueryAsync<Image>("SELECT * FROM Images"))
                .ToArray();

            images.Should().HaveCount(imagesCount);

            folders = (await connection.QueryAsync<Folder>("SELECT * FROM Folders"))
                .ToArray();

            folders.Should().HaveCount(directoryCount);

            exifDatas = (await connection.QueryAsync("SELECT * FROM ExifData"))
                .ToArray();

            exifDatas.Should().HaveCount(imagesCount);
        }

        [Fact]
        public async Task ShouldScanAndUpdateImage()
        {
            await InitializeDataContextAsync();
            await using var connection = DataContext.Database.GetDbConnection();

            var imagesCount = 5;
            var generatedImages = await GenerateImagesAsync(imagesCount);

            var imagesDirectoryInfo = FileSystem.DirectoryInfo.FromDirectoryName(ImagesPath);
            var directoryCount = imagesDirectoryInfo.EnumerateDirectories().Count();

            var imageContainerOperationService = Container.Resolve<ImageContainerOperationService>();
            var scannedImageRefs = new ObservableCollection<ImageRef>();

            imageContainerOperationService.ScanImageObservable
                .Subscribe(imageRef =>
                {
                    scannedImageRefs.Add(imageRef);
                });

            await imageContainerOperationService
                .ScanFolder(ImagesPath, new SourceCache<ImageRef, string>(imageRef => imageRef.ImagePath));

            scannedImageRefs
                .WhenAny(refs => refs.Count, change => change.Value)
                .Subscribe(i =>
                {
                    if (i == imagesCount)
                    {
                        Set();
                    }
                });

            WaitOne(45);

            scannedImageRefs.Should().HaveCount(imagesCount);

            var images = (await connection.QueryAsync<Image>("SELECT * FROM Images"))
                .ToArray();

            images.Should().HaveCount(imagesCount);

            var folders = (await connection.QueryAsync<Folder>("SELECT * FROM Folders"))
                .ToArray();

            folders.Should().HaveCount(directoryCount);

            var exifDatas = (await connection.QueryAsync("SELECT * FROM ExifData"))
                .ToArray();

            exifDatas.Should().HaveCount(imagesCount);

            var containers = await imageContainerOperationService
                .GetAllImageContainers()
                .ToArray();

            containers.Length
                .Should()
                .Be(directoryCount);

            containers.SelectMany(container => container.ImageRefs)
                .Should().HaveCount(imagesCount);

            var imageToUpdate = Faker.PickRandom(images);
            var imageToUpdateExifData = exifDatas.First(o => imageToUpdate.ExifDataId == (int) o.Id);

            var fileInfo = FileSystem.FileInfo.FromFileName(imageToUpdate.Path);

            var generatedImages2 = await GenerateImagesAsync(1, fileInfo.DirectoryName);
            var imagePathToUpdateWith = generatedImages2.First().Value.First();

            FileSystem.File.Delete(imageToUpdate.Path);
            FileSystem.File.Move(imagePathToUpdateWith, imageToUpdate.Path);

            var imageContainer = await imageContainerOperationService.UpdateImage(imageToUpdate.Path);

            images = (await connection.QueryAsync<Image>("SELECT * FROM Images"))
                .ToArray();

            images.Should().HaveCount(imagesCount);

            folders = (await connection.QueryAsync<Folder>("SELECT * FROM Folders"))
                .ToArray();

            folders.Should().HaveCount(directoryCount);

            exifDatas = (await connection.QueryAsync("SELECT * FROM ExifData"))
                .ToArray();

            exifDatas.Should().HaveCount(imagesCount);

            var updatedExifData = exifDatas.First(o => (int) imageToUpdateExifData.Id == (int) o.Id);

            ((object) imageToUpdateExifData)
                .Should()
                .NotBeEquivalentTo((object) updatedExifData);
        }

        [Fact]
        public async Task ShouldScanAndAddImageToNewFolder()
        {
            await InitializeDataContextAsync();
            await using var connection = DataContext.Database.GetDbConnection();

            var imagesCount = 25;
            await GenerateImagesAsync(imagesCount);

            var imagesDirectoryInfo = FileSystem.DirectoryInfo.FromDirectoryName(ImagesPath);
            var directoryCount = imagesDirectoryInfo.EnumerateDirectories().Count();

            var imageContainerOperationService = Container.Resolve<ImageContainerOperationService>();
            var scannedImageRefs = new ObservableCollection<ImageRef>();

            imageContainerOperationService.ScanImageObservable
                .Subscribe(imageRef =>
                {
                    scannedImageRefs.Add(imageRef);
                });

            scannedImageRefs
                .WhenAny(refs => refs.Count, change => change.Value)
                .Subscribe(i =>
                {
                    if (i == imagesCount)
                    {
                        Set();
                    }
                });

            await imageContainerOperationService
                .ScanFolder(ImagesPath, new SourceCache<ImageRef, string>(imageRef => imageRef.ImagePath));

            WaitOne(25);

            scannedImageRefs.Should().HaveCount(imagesCount);

            var images = (await connection.QueryAsync<Image>("SELECT * FROM Images"))
                .ToArray();

            images.Should().HaveCount(imagesCount);

            var folders = (await connection.QueryAsync<Folder>("SELECT * FROM Folders"))
                .ToArray();

            folders.Should().HaveCount(directoryCount);

            var exifDatas = (await connection.QueryAsync("SELECT * FROM ExifData"))
                .ToArray();

            exifDatas.Should().HaveCount(imagesCount);

            var containers = await imageContainerOperationService
                .GetAllImageContainers()
                .ToArray();

            containers.Length
                .Should()
                .Be(directoryCount);

            containers.SelectMany(container => container.ImageRefs)
                .Should().HaveCount(imagesCount);

            var customPath = FileSystem.Path.Combine(ImagesPath, "CustomPath");
            FileSystem.Directory.CreateDirectory(customPath);

            var generatedData = await GenerateImagesAsync(1, customPath);
            imagesCount = imagesCount + 1;

            var file = generatedData.First().Value.First();
            var addedImageContainers = await imageContainerOperationService.AddImage(file)
                .ToArray();

            addedImageContainers.Should().ContainSingle();

            directoryCount = imagesDirectoryInfo.EnumerateDirectories().Count();

            var imagesAfter = (await connection.QueryAsync<Image>("SELECT * FROM Images"))
                .ToArray();

            imagesAfter.Should().HaveCount(imagesCount);

            imagesAfter.FirstOrDefault(image => image.Path == file)
                .Should().NotBeNull();

            var foldersAfter = (await connection.QueryAsync<Folder>("SELECT * FROM Folders"))
                .ToArray();

            foldersAfter.Should().HaveCount(directoryCount);

            foldersAfter.FirstOrDefault(folder => folder.Path == customPath)
                .Should().NotBeNull();

            var exifDatasAfter = (await connection.QueryAsync("SELECT * FROM ExifData"))
                .ToArray();

            exifDatasAfter.Should().HaveCount(imagesCount);

            containers = await imageContainerOperationService
                .GetAllImageContainers()
                .ToArray();

            containers.Length
                .Should()
                .Be(directoryCount);

            containers.SelectMany(container => container.ImageRefs)
                .Should().HaveCount(imagesCount);
        }

        [Fact]
        public async Task ShouldScanAndAddImageToExistingFolder()
        {
            await InitializeDataContextAsync();
            await using var connection = DataContext.Database.GetDbConnection();

            var imagesCount = 5;
            await GenerateImagesAsync(imagesCount);

            var imagesDirectoryInfo = FileSystem.DirectoryInfo.FromDirectoryName(ImagesPath);
            var directoryCount = imagesDirectoryInfo.EnumerateDirectories().Count();

            var imageContainerOperationService = Container.Resolve<ImageContainerOperationService>();
            var scannedImageRefs = new ObservableCollection<ImageRef>();

            imageContainerOperationService.ScanImageObservable
                .Subscribe(imageRef =>
                {
                    scannedImageRefs.Add(imageRef);
                });

            await imageContainerOperationService
                .ScanFolder(ImagesPath, new SourceCache<ImageRef, string>(imageRef => imageRef.ImagePath));

            scannedImageRefs
                .WhenAny(refs => refs.Count, change => change.Value)
                .Subscribe(i =>
                {
                    if (i == imagesCount)
                    {
                        Set();
                    }
                });

            WaitOne(45);

            scannedImageRefs.Should().HaveCount(imagesCount);

            var images = (await connection.QueryAsync<Image>("SELECT * FROM Images"))
                .ToArray();

            images.Should().HaveCount(imagesCount);

            var folders = (await connection.QueryAsync<Folder>("SELECT * FROM Folders"))
                .ToArray();

            folders.Should().HaveCount(directoryCount);

            var exifDatas = (await connection.QueryAsync("SELECT * FROM ExifData"))
                .ToArray();

            exifDatas.Should().HaveCount(imagesCount);

            var containers = await imageContainerOperationService
                .GetAllImageContainers()
                .ToArray();

            containers.Length
                .Should()
                .Be(directoryCount);

            containers.SelectMany(container => container.ImageRefs)
                .Should().HaveCount(imagesCount);

            var existingDirectory = Faker.PickRandom(imagesDirectoryInfo.EnumerateDirectories()).FullName;

            var generatedData = await GenerateImagesAsync(1, existingDirectory);
            imagesCount = imagesCount + 1;

            var file = generatedData.First().Value.First();
            var addedImageContainers = await imageContainerOperationService.AddImage(file)
                .ToArray();

            addedImageContainers.Should().ContainSingle();

            directoryCount = imagesDirectoryInfo.EnumerateDirectories().Count();

            images = (await connection.QueryAsync<Image>("SELECT * FROM Images"))
                .ToArray();

            images.Should().HaveCount(imagesCount);

            folders = (await connection.QueryAsync<Folder>("SELECT * FROM Folders"))
                .ToArray();

            folders.Should().HaveCount(directoryCount);

            exifDatas = (await connection.QueryAsync("SELECT * FROM ExifData"))
                .ToArray();

            exifDatas.Should().HaveCount(imagesCount);

            containers = await imageContainerOperationService
                .GetAllImageContainers()
                .ToArray();

            containers.Length
                .Should()
                .Be(directoryCount);

            containers.SelectMany(container => container.ImageRefs)
                .Should().HaveCount(imagesCount);
        }

        [Fact]
        public async Task ShouldCreateAndDeleteAlbum()
        {
            await InitializeDataContextAsync();
            await using var connection = DataContext.Database.GetDbConnection();

            var imagesCount = 10;
            var albumImageCount = 5;

            await GenerateImagesAsync(imagesCount);

            var imageContainerOperationService = Container.Resolve<ImageContainerOperationService>();

            var scannedImageRefs = new ObservableCollection<ImageRef>();

            imageContainerOperationService.ScanImageObservable
                .Subscribe(imageRef =>
                {
                    scannedImageRefs.Add(imageRef);
                });

            await imageContainerOperationService
                .ScanFolder(ImagesPath, new SourceCache<ImageRef, string>(imageRef => imageRef.ImagePath));

            scannedImageRefs
                .WhenAny(refs => refs.Count, change => change.Value)
                .Subscribe(i =>
                {
                    if (i == imagesCount)
                    {
                        Set();
                    }
                });

            WaitOne(45);

            scannedImageRefs.Should().HaveCount(imagesCount);

            var imageContainers = await scannedImageRefs
                .Select(i => imageContainerOperationService.GetFolderImageContainer(i.ContainerId))
                .Concat()
                .ToArray();

            var selection = Faker
                .PickRandom(
                    imageContainers.SelectMany(container => container.ImageRefs.Select(imageRef => imageRef)),
                    albumImageCount)
                .ToArray();

            var selectionIds = selection.Select(imageRef => imageRef.Id)
                .ToArray();

            ICreateAlbum createAlbum = new TestCreateAlbum
            {
                AlbumName = Faker.Random.Word(),
                AlbumDate = selection
                    .Select(imageRef => imageRef.ContainerDate)
                    .Min()
                    .Date
            };

            var imageContainer = await imageContainerOperationService.CreateAlbum(createAlbum);
            imageContainer.Name.Should().Be(createAlbum.AlbumName);
            imageContainer.Date.Should().Be(createAlbum.AlbumDate);
            imageContainer.ContainerType.Should().Be(ImageContainerTypeEnum.Album);
            imageContainer.ImageRefs.Should().BeEmpty();
            imageContainer.Year.Should().Be(createAlbum.AlbumDate.Year);

            imageContainer = await imageContainerOperationService.AddImagesToAlbum(imageContainer.Id, selectionIds);
            imageContainer.Name.Should().Be(createAlbum.AlbumName);
            imageContainer.Date.Should().Be(createAlbum.AlbumDate);
            imageContainer.ContainerType.Should().Be(ImageContainerTypeEnum.Album);
            imageContainer.ImageRefs.Should().HaveCount(albumImageCount);
            imageContainer.Year.Should().Be(createAlbum.AlbumDate.Year);

            var albums = (await connection.QueryAsync<Album>("SELECT * FROM Albums"))
                .ToArray();

            albums.Should().ContainSingle();

            var albumImages = (await connection.QueryAsync<AlbumImage>("SELECT * FROM AlbumImages"))
                .ToArray();

            albumImages.Should().HaveCount(5);

            imageContainerOperationService.DeleteAlbum(imageContainer.Id)
                .Subscribe(unit =>
                {
                    AutoResetEvent.Set();
                });

            WaitOne();

            albums = (await connection.QueryAsync<Album>("SELECT * FROM Albums"))
                .ToArray();

            albums.Should().BeEmpty();

            albumImages = (await connection.QueryAsync<AlbumImage>("SELECT * FROM AlbumImages"))
                .ToArray();

            albumImages.Should().BeEmpty();
        }

        [Fact]
        public async Task ShouldCreateAndGetAllContainers()
        {
            await InitializeDataContextAsync();
            await using var connection = DataContext.Database.GetDbConnection();

            var imagesCount = 10;
            var albumImageCount = 5;

            var generateImages = await GenerateImagesAsync(imagesCount);

            var imageContainerOperationService = Container.Resolve<ImageContainerOperationService>();

            var scannedImageRefs = new ObservableCollection<ImageRef>();

            imageContainerOperationService.ScanImageObservable
                .Subscribe(imageRef =>
                {
                    scannedImageRefs.Add(imageRef);
                });

            await imageContainerOperationService
                .ScanFolder(ImagesPath, new SourceCache<ImageRef, string>(imageRef => imageRef.ImagePath));

            scannedImageRefs
                .WhenAny(refs => refs.Count, change => change.Value)
                .Subscribe(i =>
                {
                    if (i == imagesCount)
                    {
                        Set();
                    }
                });

            WaitOne(45);

            scannedImageRefs.Should().HaveCount(imagesCount);
            var imageContainerIds = scannedImageRefs
                .Select(imageRef => imageRef.ContainerId)
                .Distinct()
                .ToArray();

            imageContainerIds.Should().HaveCount(generateImages.Count);

            var imageRefs = Faker
                .PickRandom(scannedImageRefs, albumImageCount)
                .ToArray();

            var imageIds = imageRefs.Select(imageRef => imageRef.Id)
                .ToArray();

            ICreateAlbum createAlbum = new TestCreateAlbum
            {
                AlbumName = Faker.Random.Word(),
                AlbumDate = imageRefs
                    .Select(imageRef => imageRef.ContainerDate)
                    .Min()
                    .Date
            };

            var imageContainer = await imageContainerOperationService.CreateAlbum(createAlbum);
            var imageContainer2 = await imageContainerOperationService.AddImagesToAlbum(imageContainer.Id, imageIds);

            imageContainer.Id.Should().Be(imageContainer2.Id);

            var albums = (await connection.QueryAsync<Album>("SELECT * FROM Albums"))
                .ToArray();

            albums.Should().ContainSingle();

            var albumImages = (await connection.QueryAsync<AlbumImage>("SELECT * FROM AlbumImages"))
                .ToArray();

            albumImages.Should().HaveCount(5);

            var imageContainersAfter = await imageContainerOperationService.GetAllImageContainers()
                .ToArray();

            imageContainersAfter
                .Should()
                .HaveCount(imageContainerIds.Length + 1);

            var albumImageContainer =
                imageContainersAfter.First(container => container.ContainerType == ImageContainerTypeEnum.Album);
            albumImageContainer.Name.Should().Be(createAlbum.AlbumName);
            albumImageContainer.Date.Should().Be(createAlbum.AlbumDate);
        }

        [Fact]
        public async Task ShouldScanAndRescan()
        {
            await InitializeDataContextAsync();
            await using var connection = DataContext.Database.GetDbConnection();

            var imagesCount = 5;
            var addImages = 5;
            var generatedImages = await GenerateImagesAsync(imagesCount);

            var imagesDirectoryInfo = FileSystem.DirectoryInfo.FromDirectoryName(ImagesPath);
            var directoryCount = imagesDirectoryInfo.EnumerateDirectories().Count();

            var folderImageRefCache = new SourceCache<ImageRef, string>(imageRef => imageRef.ImagePath);

            var imageContainerOperationService = Container.Resolve<ImageContainerOperationService>();
            var scannedImageRefs = new ObservableCollection<ImageRef>();

            imageContainerOperationService.ScanImageObservable
                .Subscribe(imageRef =>
                {
                    scannedImageRefs.Add(imageRef);
                });

            await imageContainerOperationService
                .ScanFolder(ImagesPath, new SourceCache<ImageRef, string>(imageRef => imageRef.ImagePath));

            var disposable = scannedImageRefs
                .WhenAny(refs => refs.Count, change => change.Value)
                .Subscribe(i =>
                {
                    if (i == imagesCount)
                    {
                        Set();
                    }
                });

            WaitOne(45);
            disposable.Dispose();

            scannedImageRefs.Should().HaveCount(imagesCount);

            var images = (await connection.QueryAsync<Image>("SELECT * FROM Images"))
                .ToArray();

            images.Should().HaveCount(imagesCount);

            var exifDatas = (await connection.QueryAsync("SELECT * FROM ExifData"))
                .ToArray();

            exifDatas.Should().HaveCount(imagesCount);

            var folders = (await connection.QueryAsync<Folder>("SELECT * FROM Folders"))
                .ToArray();

            folders.Should().HaveCount(directoryCount);

            var containers = await imageContainerOperationService
                .GetAllImageContainers()
                .ToArray();

            containers.Length
                .Should()
                .Be(directoryCount);

            var imageRefs = generatedImages.SelectMany(pair => pair.Value)
                .Select(s =>
                {
                    var imageRef = Fakers.ImageRefFaker.Generate();
                    imageRef.ImagePath = s;
                    return imageRef;
                })
                .ToArray();

            foreach (var imageRef in imageRefs)
            {
                folderImageRefCache.AddOrUpdate(imageRef);
            }

            await GenerateImagesAsync(addImages);
            imagesCount += addImages;

            disposable = scannedImageRefs
                .WhenAny(refs => refs.Count, change => change.Value)
                .Subscribe(i =>
                {
                    if (i == imagesCount)
                    {
                        Set();
                    }
                });

            await imageContainerOperationService
                .ScanFolder(ImagesPath, folderImageRefCache)
                .ToArray();

            WaitOne(45);
            disposable.Dispose();

            directoryCount = imagesDirectoryInfo.EnumerateDirectories().Count();

            images = (await connection.QueryAsync<Image>("SELECT * FROM Images"))
                .ToArray();

            images.Should().HaveCount(imagesCount);

            exifDatas = (await connection.QueryAsync("SELECT * FROM ExifData"))
                .ToArray();

            exifDatas.Should().HaveCount(imagesCount);

            folders = (await connection.QueryAsync<Folder>("SELECT * FROM Folders"))
                .ToArray();

            folders.Should().HaveCount(directoryCount);

            containers = await imageContainerOperationService
                .GetAllImageContainers()
                .ToArray();

            containers.Length
                .Should()
                .Be(directoryCount);
        }
    }
}