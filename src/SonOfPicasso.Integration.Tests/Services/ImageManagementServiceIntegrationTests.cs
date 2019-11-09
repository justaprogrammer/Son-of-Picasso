using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Autofac;
using AutofacSerilogIntegration;
using Dapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
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

            _imageGenerationService = _container.Resolve<ImageGenerationService>();
            _imagesPath = FileSystem.Path.Combine(TestPath, "Images");
            FileSystem.Directory.CreateDirectory(_imagesPath);
        }

        public override void Dispose()
        {
            base.Dispose();

            _container.Dispose();
        }

        private async Task<IDictionary<string, string[]>> GenerateImages(int count, string imagesPath = null)
        {
            return await _imageGenerationService.GenerateImages(count, imagesPath ?? _imagesPath, imagesPath == null)
                .SelectMany(observable => observable.ToArray().Select(items => (observable.Key, items)))
                .ToDictionary(tuple => tuple.Key, tuple => tuple.items);
        }

        private readonly IContainer _container;
        private readonly string _imagesPath;
        private readonly ImageGenerationService _imageGenerationService;

        private class TestCreateAlbum : ICreateAlbum
        {
            public string AlbumName { get; set; }

            public DateTime AlbumDate { get; set; }
        }

        [Fact]
        public async Task ShouldScanAndDeleteImage()
        {
            var imagesCount = 5;
            var generatedImages = await GenerateImages(imagesCount);

            var imagesDirectoryInfo = FileSystem.DirectoryInfo.FromDirectoryName(_imagesPath);
            var directoryCount = imagesDirectoryInfo.EnumerateDirectories().Count();

            var imageManagementService = _container.Resolve<ImageManagementService>();
            var imageContainers = await imageManagementService
                .ScanFolder(_imagesPath)
                .ToArray();

            imageContainers.Should().HaveCount(directoryCount);

            var images = (await Connection.QueryAsync<Image>("SELECT * FROM Images"))
                .ToArray();

            images.Should().HaveCount(imagesCount);

            var folders = (await Connection.QueryAsync<Folder>("SELECT * FROM Folders"))
                .ToArray();

            folders.Should().HaveCount(directoryCount);

            var exifDatas = (await Connection.QueryAsync("SELECT * FROM ExifData"))
                .ToArray();

            exifDatas.Should().HaveCount(imagesCount);

            var containers = await imageManagementService
                .GetAllImageContainers()
                .ToArray();

            containers.Length
                .Should()
                .Be(directoryCount);

            containers.SelectMany(container => container.ImageRefs)
                .Should().HaveCount(imagesCount);

            var imageToDelete = Faker.PickRandom(images);
            FileSystem.File.Delete(imageToDelete.Path);

            var deletedImageContainer = await imageManagementService.DeleteImage(imageToDelete.Path);
            deletedImageContainer.ContainerTypeId.Should().Be(imageToDelete.FolderId);

            imagesCount -= 1;

            images = (await Connection.QueryAsync<Image>("SELECT * FROM Images"))
                .ToArray();

            images.Should().HaveCount(imagesCount);

            folders = (await Connection.QueryAsync<Folder>("SELECT * FROM Folders"))
                .ToArray();

            folders.Should().HaveCount(directoryCount);

            exifDatas = (await Connection.QueryAsync("SELECT * FROM ExifData"))
                .ToArray();

            exifDatas.Should().HaveCount(imagesCount);
        }

        [Fact]
        public async Task ShouldScanAndUpdateImage()
        {
            var imagesCount = 5;
            var generatedImages = await GenerateImages(imagesCount);

            var imagesDirectoryInfo = FileSystem.DirectoryInfo.FromDirectoryName(_imagesPath);
            var directoryCount = imagesDirectoryInfo.EnumerateDirectories().Count();

            var imageManagementService = _container.Resolve<ImageManagementService>();
            var imageContainers = await imageManagementService
                .ScanFolder(_imagesPath)
                .ToArray();

            imageContainers.Should().HaveCount(directoryCount);

            var images = (await Connection.QueryAsync<Image>("SELECT * FROM Images"))
                .ToArray();

            images.Should().HaveCount(imagesCount);

            var folders = (await Connection.QueryAsync<Folder>("SELECT * FROM Folders"))
                .ToArray();

            folders.Should().HaveCount(directoryCount);

            var exifDatas = (await Connection.QueryAsync("SELECT * FROM ExifData"))
                .ToArray();

            exifDatas.Should().HaveCount(imagesCount);

            var containers = await imageManagementService
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
            
            var generatedImages2 = await GenerateImages(1, fileInfo.DirectoryName);
            var imagePathToUpdateWith = generatedImages2.First().Value.First();

            FileSystem.File.Delete(imageToUpdate.Path);
            FileSystem.File.Move(imagePathToUpdateWith, imageToUpdate.Path);

            var imageContainer = await imageManagementService.UpdateImage(imageToUpdate.Path);

            images = (await Connection.QueryAsync<Image>("SELECT * FROM Images"))
                .ToArray();

            images.Should().HaveCount(imagesCount);

            folders = (await Connection.QueryAsync<Folder>("SELECT * FROM Folders"))
                .ToArray();

            folders.Should().HaveCount(directoryCount);

            exifDatas = (await Connection.QueryAsync("SELECT * FROM ExifData"))
                .ToArray();

            exifDatas.Should().HaveCount(imagesCount);

            var updatedExifData = exifDatas.First(o => (int) imageToUpdateExifData.Id == (int) o.Id);

            ((object) imageToUpdateExifData)
                .Should()
                .NotBeEquivalentTo((object) updatedExifData);
        }

        [Fact]
        public async Task ShouldScanAndRenameImageToSameFolder()
        {
            var imagesCount = 5;
            var generatedImages = await GenerateImages(imagesCount);

            var imagesDirectoryInfo = FileSystem.DirectoryInfo.FromDirectoryName(_imagesPath);
            var directoryCount = imagesDirectoryInfo.EnumerateDirectories().Count();

            var imageManagementService = _container.Resolve<ImageManagementService>();
            var imageContainers = await imageManagementService
                .ScanFolder(_imagesPath)
                .ToArray();

            imageContainers.Should().HaveCount(directoryCount);

            var images = (await Connection.QueryAsync<Image>("SELECT * FROM Images"))
                .ToArray();

            images.Should().HaveCount(imagesCount);

            var folders = (await Connection.QueryAsync<Folder>("SELECT * FROM Folders"))
                .ToArray();

            folders.Should().HaveCount(directoryCount);

            var exifDatas = (await Connection.QueryAsync("SELECT * FROM ExifData"))
                .ToArray();

            exifDatas.Should().HaveCount(imagesCount);

            var containers = await imageManagementService
                .GetAllImageContainers()
                .ToArray();

            containers.Length
                .Should()
                .Be(directoryCount);

            containers.SelectMany(container => container.ImageRefs)
                .Should().HaveCount(imagesCount);

            var imageToRename = Faker.PickRandom(images);

            var directoryName = FileSystem.Path.GetDirectoryName(imageToRename.Path);
            var imageExtension = FileSystem.Path.GetExtension(imageToRename.Path);
            var renamePath = FileSystem.Path.Combine(directoryName, Faker.System.FileName(imageExtension));

            FileSystem.File.Move(imageToRename.Path, renamePath);

            var updatedImageContainers = await imageManagementService.RenameImage(imageToRename.Path, renamePath).ToArray();
            updatedImageContainers.Should().ContainSingle();
            updatedImageContainers.First().ContainerTypeId.Should().Be(imageToRename.FolderId);

            images = (await Connection.QueryAsync<Image>("SELECT * FROM Images"))
                .ToArray();

            images.Should().HaveCount(imagesCount);

            folders = (await Connection.QueryAsync<Folder>("SELECT * FROM Folders"))
                .ToArray();

            folders.Should().HaveCount(directoryCount);

            exifDatas = (await Connection.QueryAsync("SELECT * FROM ExifData"))
                .ToArray();

            exifDatas.Should().HaveCount(imagesCount);

            var renamedImage = (await Connection.QueryAsync<Image>("SELECT * FROM Images WHERE Images.Path=@Path",
                    new {Path = renamePath}))
                .First();

            imageToRename.FolderId
                .Should()
                .Be(renamedImage.FolderId);
        }

        [Fact]
        public async Task ShouldScanAndRenameImageToOtherFolder()
        {
            var imagesCount = 15;
            var generatedImages = await GenerateImages(imagesCount);

            var imagesDirectoryInfo = FileSystem.DirectoryInfo.FromDirectoryName(_imagesPath);
            var directoryCount = imagesDirectoryInfo.EnumerateDirectories().Count();

            var imageManagementService = _container.Resolve<ImageManagementService>();
            var imageContainers = await imageManagementService
                .ScanFolder(_imagesPath)
                .ToArray();

            imageContainers.Should().HaveCount(directoryCount);

            var images = (await Connection.QueryAsync<Image>("SELECT * FROM Images"))
                .ToArray();

            images.Should().HaveCount(imagesCount);

            var folders = (await Connection.QueryAsync<Folder>("SELECT * FROM Folders"))
                .ToArray();

            folders.Should().HaveCount(directoryCount);

            var exifDatas = (await Connection.QueryAsync("SELECT * FROM ExifData"))
                .ToArray();

            exifDatas.Should().HaveCount(imagesCount);

            var containers = await imageManagementService
                .GetAllImageContainers()
                .ToArray();

            containers.Length
                .Should()
                .Be(directoryCount);

            containers.SelectMany(container => container.ImageRefs)
                .Should().HaveCount(imagesCount);

            var imageToRename = images.First(i => i.Path == generatedImages.First().Value.First());
            var imageExtension = FileSystem.Path.GetExtension(imageToRename.Path);
            
            var folderPathToMoveTo = generatedImages.Skip(1).First().Key;
            var folderToMoveTo = (await Connection.QueryAsync<Folder>("SELECT * FROM Folders WHERE Folders.Path=@Path",
                    new {Path = folderPathToMoveTo}))
                .First();

            imageToRename.FolderId
                .Should()
                .NotBe(folderToMoveTo.Id);

            var renamePath = FileSystem.Path.Combine(folderPathToMoveTo, Faker.System.FileName(imageExtension));

            FileSystem.File.Move(imageToRename.Path, renamePath);

            var updatedImageContainers = await imageManagementService.RenameImage(imageToRename.Path, renamePath).ToArray();
            updatedImageContainers.Should().HaveCount(2);
            updatedImageContainers.Select(container => container.ContainerTypeId)
                .Should()
                .BeEquivalentTo(new[] {imageToRename.FolderId, folderToMoveTo.Id});

            var imagesAfter = (await Connection.QueryAsync<Image>("SELECT * FROM Images"))
                .ToArray();

            imagesAfter.Should().HaveCount(imagesCount);

            var foldersAfter = (await Connection.QueryAsync<Folder>("SELECT * FROM Folders"))
                .ToArray();

            foldersAfter.Should().HaveCount(directoryCount);

            var exifDatasAfter = (await Connection.QueryAsync("SELECT * FROM ExifData"))
                .ToArray();

            exifDatasAfter.Should().HaveCount(imagesCount);

            var renamedImage = (await Connection.QueryAsync<Image>("SELECT * FROM Images WHERE Images.Path=@Path",
                    new {Path = renamePath}))
                .First();

            imageToRename.FolderId
                .Should()
                .NotBe(renamedImage.FolderId);

            renamedImage.FolderId
                .Should()
                .Be(folderToMoveTo.Id);
        }

        [Fact]
        public async Task ShouldScanAndRenameImageToNewFolder()
        {
            var imagesCount = 3;
            var generatedImages = await GenerateImages(imagesCount);

            var imagesDirectoryInfo = FileSystem.DirectoryInfo.FromDirectoryName(_imagesPath);
            var directoryCount = imagesDirectoryInfo.EnumerateDirectories().Count();

            var imageManagementService = _container.Resolve<ImageManagementService>();
            var imageContainers = await imageManagementService
                .ScanFolder(_imagesPath)
                .ToArray();

            imageContainers.Should().HaveCount(directoryCount);

            var images = (await Connection.QueryAsync<Image>("SELECT * FROM Images"))
                .ToArray();

            images.Should().HaveCount(imagesCount);

            var folders = (await Connection.QueryAsync<Folder>("SELECT * FROM Folders"))
                .ToArray();

            folders.Should().HaveCount(directoryCount);

            var exifDatas = (await Connection.QueryAsync("SELECT * FROM ExifData"))
                .ToArray();

            exifDatas.Should().HaveCount(imagesCount);

            var containers = await imageManagementService
                .GetAllImageContainers()
                .ToArray();

            containers.Length
                .Should()
                .Be(directoryCount);

            containers.SelectMany(container => container.ImageRefs)
                .Should().HaveCount(imagesCount);

            var imageToRename = images.First(i => i.Path == generatedImages.First().Value.First());
            var imageExtension = FileSystem.Path.GetExtension(imageToRename.Path);
            
            var folderPathToMoveTo = FileSystem.Path.Combine(_imagesPath, "Custom");
            FileSystem.Directory.CreateDirectory(folderPathToMoveTo);
            
            var renamePath = FileSystem.Path.Combine(folderPathToMoveTo, Faker.System.FileName(imageExtension));

            FileSystem.File.Move(imageToRename.Path, renamePath);

            var updatedImageContainers = await imageManagementService.RenameImage(imageToRename.Path, renamePath).ToArray();
            updatedImageContainers.Should().HaveCount(2);

            var imagesAfter = (await Connection.QueryAsync<Image>("SELECT * FROM Images"))
                .ToArray();

            imagesAfter.Should().HaveCount(imagesCount);

            var foldersAfter = (await Connection.QueryAsync<Folder>("SELECT * FROM Folders"))
                .ToArray();

            foldersAfter.Should().HaveCount(directoryCount + 1);

            var exifDatasAfter = (await Connection.QueryAsync("SELECT * FROM ExifData"))
                .ToArray();

            exifDatasAfter.Should().HaveCount(imagesCount);

            var renamedImage = (await Connection.QueryAsync<Image>("SELECT * FROM Images WHERE Images.Path=@Path",
                    new {Path = renamePath}))
                .First();

            imageToRename.FolderId
                .Should()
                .NotBe(renamedImage.FolderId);
        }

        [Fact]
        public async Task ShouldScanAndAddImageToNewFolder()
        {
            var imagesCount = 50;
            await GenerateImages(imagesCount);

            var imagesDirectoryInfo = FileSystem.DirectoryInfo.FromDirectoryName(_imagesPath);
            var directoryCount = imagesDirectoryInfo.EnumerateDirectories().Count();

            var imageManagementService = _container.Resolve<ImageManagementService>();
            var imageContainers = await imageManagementService
                .ScanFolder(_imagesPath)
                .ToArray();

            imageContainers.Should().HaveCount(directoryCount);

            var images = (await Connection.QueryAsync<Image>("SELECT * FROM Images"))
                .ToArray();

            images.Should().HaveCount(imagesCount);

            var folders = (await Connection.QueryAsync<Folder>("SELECT * FROM Folders"))
                .ToArray();

            folders.Should().HaveCount(directoryCount);

            var exifDatas = (await Connection.QueryAsync("SELECT * FROM ExifData"))
                .ToArray();

            exifDatas.Should().HaveCount(imagesCount);

            var containers = await imageManagementService
                .GetAllImageContainers()
                .ToArray();

            containers.Length
                .Should()
                .Be(directoryCount);

            containers.SelectMany(container => container.ImageRefs)
                .Should().HaveCount(imagesCount);

            var customPath = FileSystem.Path.Combine(_imagesPath, "CustomPath");
            FileSystem.Directory.CreateDirectory(customPath);

            var generatedData = await GenerateImages(1, customPath);
            imagesCount = imagesCount + 1;

            var file = generatedData.First().Value.First();
            var addedImageContainers = await imageManagementService.AddImage(file)
                .ToArray();

            addedImageContainers.Should().ContainSingle();

            directoryCount = imagesDirectoryInfo.EnumerateDirectories().Count();

            images = (await Connection.QueryAsync<Image>("SELECT * FROM Images"))
                .ToArray();

            images.Should().HaveCount(imagesCount);

            folders = (await Connection.QueryAsync<Folder>("SELECT * FROM Folders"))
                .ToArray();

            folders.Should().HaveCount(directoryCount);

            exifDatas = (await Connection.QueryAsync("SELECT * FROM ExifData"))
                .ToArray();

            exifDatas.Should().HaveCount(imagesCount);

            containers = await imageManagementService
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
            var imagesCount = 5;
            await GenerateImages(imagesCount);

            var imagesDirectoryInfo = FileSystem.DirectoryInfo.FromDirectoryName(_imagesPath);
            var directoryCount = imagesDirectoryInfo.EnumerateDirectories().Count();

            var imageManagementService = _container.Resolve<ImageManagementService>();
            var imageContainers = await imageManagementService
                .ScanFolder(_imagesPath)
                .ToArray();

            imageContainers.Should().HaveCount(directoryCount);

            var images = (await Connection.QueryAsync<Image>("SELECT * FROM Images"))
                .ToArray();

            images.Should().HaveCount(imagesCount);

            var folders = (await Connection.QueryAsync<Folder>("SELECT * FROM Folders"))
                .ToArray();

            folders.Should().HaveCount(directoryCount);

            var exifDatas = (await Connection.QueryAsync("SELECT * FROM ExifData"))
                .ToArray();

            exifDatas.Should().HaveCount(imagesCount);

            var containers = await imageManagementService
                .GetAllImageContainers()
                .ToArray();

            containers.Length
                .Should()
                .Be(directoryCount);

            containers.SelectMany(container => container.ImageRefs)
                .Should().HaveCount(imagesCount);

            var existingDirectory = Faker.PickRandom(imagesDirectoryInfo.EnumerateDirectories()).FullName;

            var generatedData = await GenerateImages(1, existingDirectory);
            imagesCount = imagesCount + 1;

            var file = generatedData.First().Value.First();
            var addedImageContainers = await imageManagementService.AddImage(file)
                .ToArray();

            addedImageContainers.Should().ContainSingle();

            directoryCount = imagesDirectoryInfo.EnumerateDirectories().Count();

            images = (await Connection.QueryAsync<Image>("SELECT * FROM Images"))
                .ToArray();

            images.Should().HaveCount(imagesCount);

            folders = (await Connection.QueryAsync<Folder>("SELECT * FROM Folders"))
                .ToArray();

            folders.Should().HaveCount(directoryCount);

            exifDatas = (await Connection.QueryAsync("SELECT * FROM ExifData"))
                .ToArray();

            exifDatas.Should().HaveCount(imagesCount);

            containers = await imageManagementService
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
            var imageCount = 10;
            var albumImageCount = 5;

            await GenerateImages(imageCount);

            var imageManagementService = _container.Resolve<ImageManagementService>();
            var imageContainers = await imageManagementService
                .ScanFolder(_imagesPath)
                .ToArray();

            var imageRefs = Faker
                .PickRandom(imageContainers.SelectMany(container => container.ImageRefs.Select(imageRef => imageRef)),
                    albumImageCount)
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

            imageManagementService.DeleteAlbum(imageContainer.ContainerTypeId)
                .Subscribe(unit =>
                {
                    AutoResetEvent.Set();
                });

            WaitOne();

            albums = (await Connection.QueryAsync<Album>("SELECT * FROM Albums"))
                .ToArray();

            albums.Should().BeEmpty();

            albumImages = (await Connection.QueryAsync<AlbumImage>("SELECT * FROM AlbumImages"))
                .ToArray();

            albumImages.Should().BeEmpty();
        }

        [Fact]
        public async Task ShouldCreateAndGetAllContainers()
        {
            var imageCount = 10;
            var albumImageCount = 5;

            await GenerateImages(imageCount);

            var imageManagementService = _container.Resolve<ImageManagementService>();
            var imageContainers = await imageManagementService
                .ScanFolder(_imagesPath)
                .ToArray();

            var imageRefs = Faker
                .PickRandom(imageContainers.SelectMany(container => container.ImageRefs.Select(imageRef => imageRef)),
                    albumImageCount)
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
            imageContainer = await imageManagementService.AddImagesToAlbum(imageContainer.ContainerTypeId, imageIds);

            var albums = (await Connection.QueryAsync<Album>("SELECT * FROM Albums"))
                .ToArray();

            albums.Should().ContainSingle();

            var albumImages = (await Connection.QueryAsync<AlbumImage>("SELECT * FROM AlbumImages"))
                .ToArray();

            albumImages.Should().HaveCount(5);

            var imageContainersAfter = await imageManagementService.GetAllImageContainers()
                .ToArray();

            imageContainersAfter
                .Should()
                .HaveCount(imageContainers.Length + 1);

            var albumImageContainer = imageContainersAfter.First(container => container.ContainerType == ImageContainerTypeEnum.Album);
            albumImageContainer.Name.Should().Be(createAlbum.AlbumName);
            albumImageContainer.Date.Should().Be(createAlbum.AlbumDate);
        }

        [Fact]
        public async Task ShouldScanAndRescan()
        {
            var imageCount = 5;
            var addImages = 5;
            await GenerateImages(imageCount);

            var imagesDirectoryInfo = FileSystem.DirectoryInfo.FromDirectoryName(_imagesPath);
            var directoryCount = imagesDirectoryInfo.EnumerateDirectories().Count();

            var imageManagementService = _container.Resolve<ImageManagementService>();
            var imageContainers = await imageManagementService
                .ScanFolder(_imagesPath)
                .ToArray();

            imageContainers.Should().HaveCount(directoryCount);

            var images = (await Connection.QueryAsync<Image>("SELECT * FROM Images"))
                .ToArray();

            images.Should().HaveCount(imageCount);

            var exifDatas = (await Connection.QueryAsync("SELECT * FROM ExifData"))
                .ToArray();

            exifDatas.Should().HaveCount(imageCount);

            var folders = (await Connection.QueryAsync<Folder>("SELECT * FROM Folders"))
                .ToArray();

            folders.Should().HaveCount(directoryCount);

            var containers = await imageManagementService
                .GetAllImageContainers()
                .ToArray();

            containers.Length
                .Should()
                .Be(directoryCount);

            await GenerateImages(addImages);
            imageCount += addImages;

            await imageManagementService
                .ScanFolder(_imagesPath)
                .ToArray();

            directoryCount = imagesDirectoryInfo.EnumerateDirectories().Count();

            images = (await Connection.QueryAsync<Image>("SELECT * FROM Images"))
                .ToArray();

            images.Should().HaveCount(imageCount);

            exifDatas = (await Connection.QueryAsync("SELECT * FROM ExifData"))
                .ToArray();

            exifDatas.Should().HaveCount(imageCount);

            folders = (await Connection.QueryAsync<Folder>("SELECT * FROM Folders"))
                .ToArray();

            folders.Should().HaveCount(directoryCount);

            containers = await imageManagementService
                .GetAllImageContainers()
                .ToArray();

            containers.Length
                .Should()
                .Be(directoryCount);
        }
    }
}