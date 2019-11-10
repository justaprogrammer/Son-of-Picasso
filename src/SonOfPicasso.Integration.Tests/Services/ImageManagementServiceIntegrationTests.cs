﻿using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Autofac;
using Dapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SonOfPicasso.Core.Interfaces;
using SonOfPicasso.Core.Model;
using SonOfPicasso.Core.Services;
using SonOfPicasso.Data.Model;
using Xunit;
using Xunit.Abstractions;

namespace SonOfPicasso.Integration.Tests.Services
{
    public class ImageManagementServiceIntegrationTests : IntegrationTestsBase
    {
        public ImageManagementServiceIntegrationTests(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
            var containerBuilder = GetContainerBuilder();
            containerBuilder.RegisterType<ImageManagementService>();
            containerBuilder.RegisterType<ExifDataService>().As<IExifDataService>();
            containerBuilder.RegisterType<ImageLocationService>().As<IImageLocationService>();
            Container = containerBuilder.Build();
        }

        protected override IContainer Container { get; }

        public override void Dispose()
        {
            base.Dispose();

            Container.Dispose();
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

            var imageManagementService = Container.Resolve<ImageManagementService>();
            var imageContainers = await imageManagementService
                .ScanFolder(ImagesPath)
                .ToArray();

            imageContainers.Should().HaveCount(directoryCount);

            var images = (await connection.QueryAsync<Image>("SELECT * FROM Images"))
                .ToArray();

            images.Should().HaveCount(imagesCount);

            var folders = (await connection.QueryAsync<Folder>("SELECT * FROM Folders"))
                .ToArray();

            folders.Should().HaveCount(directoryCount);

            var exifDatas = (await connection.QueryAsync("SELECT * FROM ExifData"))
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

            var imageManagementService = Container.Resolve<ImageManagementService>();
            var imageContainers = await imageManagementService
                .ScanFolder(ImagesPath)
                .ToArray();

            imageContainers.Should().HaveCount(directoryCount);

            var images = (await connection.QueryAsync<Image>("SELECT * FROM Images"))
                .ToArray();

            images.Should().HaveCount(imagesCount);

            var folders = (await connection.QueryAsync<Folder>("SELECT * FROM Folders"))
                .ToArray();

            folders.Should().HaveCount(directoryCount);

            var exifDatas = (await connection.QueryAsync("SELECT * FROM ExifData"))
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

            var generatedImages2 = await GenerateImagesAsync(1, fileInfo.DirectoryName);
            var imagePathToUpdateWith = generatedImages2.First().Value.First();

            FileSystem.File.Delete(imageToUpdate.Path);
            FileSystem.File.Move(imagePathToUpdateWith, imageToUpdate.Path);

            var imageContainer = await imageManagementService.UpdateImage(imageToUpdate.Path);

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
        public async Task ShouldScanAndRenameImageToSameFolder()
        {
            await InitializeDataContextAsync();
            await using var connection = DataContext.Database.GetDbConnection();

            var imagesCount = 5;
            var generatedImages = await GenerateImagesAsync(imagesCount);

            var imagesDirectoryInfo = FileSystem.DirectoryInfo.FromDirectoryName(ImagesPath);
            var directoryCount = imagesDirectoryInfo.EnumerateDirectories().Count();

            var imageManagementService = Container.Resolve<ImageManagementService>();
            var imageContainers = await imageManagementService
                .ScanFolder(ImagesPath)
                .ToArray();

            imageContainers.Should().HaveCount(directoryCount);

            var images = (await connection.QueryAsync<Image>("SELECT * FROM Images"))
                .ToArray();

            images.Should().HaveCount(imagesCount);

            var folders = (await connection.QueryAsync<Folder>("SELECT * FROM Folders"))
                .ToArray();

            folders.Should().HaveCount(directoryCount);

            var exifDatas = (await connection.QueryAsync("SELECT * FROM ExifData"))
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

            var updatedImageContainers =
                await imageManagementService.RenameImage(imageToRename.Path, renamePath).ToArray();
            updatedImageContainers.Should().ContainSingle();
            updatedImageContainers.First().ContainerTypeId.Should().Be(imageToRename.FolderId);

            var imagesAfter = (await connection.QueryAsync<Image>("SELECT * FROM Images"))
                .ToArray();

            imagesAfter.Should().HaveCount(imagesCount);

            var foldersAfter = (await connection.QueryAsync<Folder>("SELECT * FROM Folders"))
                .ToArray();

            foldersAfter.Should().HaveCount(directoryCount);

            var exifDatasAfter = (await connection.QueryAsync("SELECT * FROM ExifData"))
                .ToArray();

            exifDatasAfter.Should().HaveCount(imagesCount);

            var renamedImage = (await connection.QueryAsync<Image>("SELECT * FROM Images WHERE Images.Path=@Path",
                    new {Path = renamePath}))
                .First();

            imageToRename.FolderId
                .Should()
                .Be(renamedImage.FolderId);
        }

        [Fact]
        public async Task ShouldScanAndRenameImageToOtherFolder()
        {
            await InitializeDataContextAsync();
            await using var connection = DataContext.Database.GetDbConnection();

            var imagesCount = 15;
            var generatedImages = await GenerateImagesAsync(imagesCount);

            var imagesDirectoryInfo = FileSystem.DirectoryInfo.FromDirectoryName(ImagesPath);
            var directoryCount = imagesDirectoryInfo.EnumerateDirectories().Count();

            var imageManagementService = Container.Resolve<ImageManagementService>();
            var imageContainers = await imageManagementService
                .ScanFolder(ImagesPath)
                .ToArray();

            imageContainers.Should().HaveCount(directoryCount);

            var images = (await connection.QueryAsync<Image>("SELECT * FROM Images"))
                .ToArray();

            images.Should().HaveCount(imagesCount);

            var folders = (await connection.QueryAsync<Folder>("SELECT * FROM Folders"))
                .ToArray();

            folders.Should().HaveCount(directoryCount);

            var exifDatas = (await connection.QueryAsync("SELECT * FROM ExifData"))
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
            var folderToMoveTo = (await connection.QueryAsync<Folder>("SELECT * FROM Folders WHERE Folders.Path=@Path",
                    new {Path = folderPathToMoveTo}))
                .First();

            imageToRename.FolderId
                .Should()
                .NotBe(folderToMoveTo.Id);

            var renamePath = FileSystem.Path.Combine(folderPathToMoveTo, Faker.System.FileName(imageExtension));

            FileSystem.File.Move(imageToRename.Path, renamePath);

            var updatedImageContainers =
                await imageManagementService.RenameImage(imageToRename.Path, renamePath).ToArray();
            updatedImageContainers.Should().HaveCount(2);
            updatedImageContainers.Select(container => container.ContainerTypeId)
                .Should()
                .BeEquivalentTo(new[] {imageToRename.FolderId, folderToMoveTo.Id});

            var imagesAfter = (await connection.QueryAsync<Image>("SELECT * FROM Images"))
                .ToArray();

            imagesAfter.Should().HaveCount(imagesCount);

            var foldersAfter = (await connection.QueryAsync<Folder>("SELECT * FROM Folders"))
                .ToArray();

            foldersAfter.Should().HaveCount(directoryCount);

            var exifDatasAfter = (await connection.QueryAsync("SELECT * FROM ExifData"))
                .ToArray();

            exifDatasAfter.Should().HaveCount(imagesCount);

            var renamedImage = (await connection.QueryAsync<Image>("SELECT * FROM Images WHERE Images.Path=@Path",
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
            await InitializeDataContextAsync();
            await using var connection = DataContext.Database.GetDbConnection();

            var imagesCount = 3;
            var generatedImages = await GenerateImagesAsync(imagesCount);

            var imagesDirectoryInfo = FileSystem.DirectoryInfo.FromDirectoryName(ImagesPath);
            var directoryCount = imagesDirectoryInfo.EnumerateDirectories().Count();

            var imageManagementService = Container.Resolve<ImageManagementService>();
            var imageContainers = await imageManagementService
                .ScanFolder(ImagesPath)
                .ToArray();

            imageContainers.Should().HaveCount(directoryCount);

            var images = (await connection.QueryAsync<Image>("SELECT * FROM Images"))
                .ToArray();

            images.Should().HaveCount(imagesCount);

            var folders = (await connection.QueryAsync<Folder>("SELECT * FROM Folders"))
                .ToArray();

            folders.Should().HaveCount(directoryCount);

            var exifDatas = (await connection.QueryAsync("SELECT * FROM ExifData"))
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

            var folderPathToMoveTo = FileSystem.Path.Combine(ImagesPath, "Custom");
            FileSystem.Directory.CreateDirectory(folderPathToMoveTo);

            var renamePath = FileSystem.Path.Combine(folderPathToMoveTo, Faker.System.FileName(imageExtension));

            FileSystem.File.Move(imageToRename.Path, renamePath);

            var updatedImageContainers =
                await imageManagementService.RenameImage(imageToRename.Path, renamePath).ToArray();
            updatedImageContainers.Should().HaveCount(2);

            var imagesAfter = (await connection.QueryAsync<Image>("SELECT * FROM Images"))
                .ToArray();

            imagesAfter.Should().HaveCount(imagesCount);

            var foldersAfter = (await connection.QueryAsync<Folder>("SELECT * FROM Folders"))
                .ToArray();

            foldersAfter.Should().HaveCount(directoryCount + 1);

            var exifDatasAfter = (await connection.QueryAsync("SELECT * FROM ExifData"))
                .ToArray();

            exifDatasAfter.Should().HaveCount(imagesCount);

            var renamedImage = (await connection.QueryAsync<Image>("SELECT * FROM Images WHERE Images.Path=@Path",
                    new {Path = renamePath}))
                .First();

            imageToRename.FolderId
                .Should()
                .NotBe(renamedImage.FolderId);
        }

        [Fact]
        public async Task ShouldScanAndAddImageToNewFolder()
        {
            await InitializeDataContextAsync();
            await using var connection = DataContext.Database.GetDbConnection();

            var imagesCount = 50;
            await GenerateImagesAsync(imagesCount);

            var imagesDirectoryInfo = FileSystem.DirectoryInfo.FromDirectoryName(ImagesPath);
            var directoryCount = imagesDirectoryInfo.EnumerateDirectories().Count();

            var imageManagementService = Container.Resolve<ImageManagementService>();
            var imageContainers = await imageManagementService
                .ScanFolder(ImagesPath)
                .ToArray();

            imageContainers.Should().HaveCount(directoryCount);

            var images = (await connection.QueryAsync<Image>("SELECT * FROM Images"))
                .ToArray();

            images.Should().HaveCount(imagesCount);

            var folders = (await connection.QueryAsync<Folder>("SELECT * FROM Folders"))
                .ToArray();

            folders.Should().HaveCount(directoryCount);

            var exifDatas = (await connection.QueryAsync("SELECT * FROM ExifData"))
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

            var customPath = FileSystem.Path.Combine(ImagesPath, "CustomPath");
            FileSystem.Directory.CreateDirectory(customPath);

            var generatedData = await GenerateImagesAsync(1, customPath);
            imagesCount = imagesCount + 1;

            var file = generatedData.First().Value.First();
            var addedImageContainers = await imageManagementService.AddImage(file)
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
            await InitializeDataContextAsync();
            await using var connection = DataContext.Database.GetDbConnection();

            var imagesCount = 5;
            await GenerateImagesAsync(imagesCount);

            var imagesDirectoryInfo = FileSystem.DirectoryInfo.FromDirectoryName(ImagesPath);
            var directoryCount = imagesDirectoryInfo.EnumerateDirectories().Count();

            var imageManagementService = Container.Resolve<ImageManagementService>();
            var imageContainers = await imageManagementService
                .ScanFolder(ImagesPath)
                .ToArray();

            imageContainers.Should().HaveCount(directoryCount);

            var images = (await connection.QueryAsync<Image>("SELECT * FROM Images"))
                .ToArray();

            images.Should().HaveCount(imagesCount);

            var folders = (await connection.QueryAsync<Folder>("SELECT * FROM Folders"))
                .ToArray();

            folders.Should().HaveCount(directoryCount);

            var exifDatas = (await connection.QueryAsync("SELECT * FROM ExifData"))
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

            var generatedData = await GenerateImagesAsync(1, existingDirectory);
            imagesCount = imagesCount + 1;

            var file = generatedData.First().Value.First();
            var addedImageContainers = await imageManagementService.AddImage(file)
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
            await InitializeDataContextAsync();
            await using var connection = DataContext.Database.GetDbConnection();

            var imageCount = 10;
            var albumImageCount = 5;

            await GenerateImagesAsync(imageCount);

            var imageManagementService = Container.Resolve<ImageManagementService>();
            var imageContainers = await imageManagementService
                .ScanFolder(ImagesPath)
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

            var albums = (await connection.QueryAsync<Album>("SELECT * FROM Albums"))
                .ToArray();

            albums.Should().ContainSingle();

            var albumImages = (await connection.QueryAsync<AlbumImage>("SELECT * FROM AlbumImages"))
                .ToArray();

            albumImages.Should().HaveCount(5);

            imageManagementService.DeleteAlbum(imageContainer.ContainerTypeId)
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

            var imageCount = 10;
            var albumImageCount = 5;

            await GenerateImagesAsync(imageCount);

            var imageManagementService = Container.Resolve<ImageManagementService>();
            var imageContainers = await imageManagementService
                .ScanFolder(ImagesPath)
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

            var albums = (await connection.QueryAsync<Album>("SELECT * FROM Albums"))
                .ToArray();

            albums.Should().ContainSingle();

            var albumImages = (await connection.QueryAsync<AlbumImage>("SELECT * FROM AlbumImages"))
                .ToArray();

            albumImages.Should().HaveCount(5);

            var imageContainersAfter = await imageManagementService.GetAllImageContainers()
                .ToArray();

            imageContainersAfter
                .Should()
                .HaveCount(imageContainers.Length + 1);

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

            var imageCount = 5;
            var addImages = 5;
            await GenerateImagesAsync(imageCount);

            var imagesDirectoryInfo = FileSystem.DirectoryInfo.FromDirectoryName(ImagesPath);
            var directoryCount = imagesDirectoryInfo.EnumerateDirectories().Count();

            var imageManagementService = Container.Resolve<ImageManagementService>();
            var imageContainers = await imageManagementService
                .ScanFolder(ImagesPath)
                .ToArray();

            imageContainers.Should().HaveCount(directoryCount);

            var images = (await connection.QueryAsync<Image>("SELECT * FROM Images"))
                .ToArray();

            images.Should().HaveCount(imageCount);

            var exifDatas = (await connection.QueryAsync("SELECT * FROM ExifData"))
                .ToArray();

            exifDatas.Should().HaveCount(imageCount);

            var folders = (await connection.QueryAsync<Folder>("SELECT * FROM Folders"))
                .ToArray();

            folders.Should().HaveCount(directoryCount);

            var containers = await imageManagementService
                .GetAllImageContainers()
                .ToArray();

            containers.Length
                .Should()
                .Be(directoryCount);

            await GenerateImagesAsync(addImages);
            imageCount += addImages;

            await imageManagementService
                .ScanFolder(ImagesPath)
                .ToArray();

            directoryCount = imagesDirectoryInfo.EnumerateDirectories().Count();

            images = (await connection.QueryAsync<Image>("SELECT * FROM Images"))
                .ToArray();

            images.Should().HaveCount(imageCount);

            exifDatas = (await connection.QueryAsync("SELECT * FROM ExifData"))
                .ToArray();

            exifDatas.Should().HaveCount(imageCount);

            folders = (await connection.QueryAsync<Folder>("SELECT * FROM Folders"))
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