using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using AutoBogus;
using Autofac.Extras.NSubstitute;
using Bogus;
using FluentAssertions;
using FluentAssertions.Execution;
using NSubstitute;
using SonOfPicasso.Core.Interfaces;
using SonOfPicasso.Core.Scheduling;
using SonOfPicasso.Core.Services;
using SonOfPicasso.Data.Model;
using SonOfPicasso.Data.Repository;
using SonOfPicasso.Testing.Common;
using SonOfPicasso.Testing.Common.Extensions;
using SonOfPicasso.Testing.Common.Scheduling;
using Xunit;
using Xunit.Abstractions;
using Directory = SonOfPicasso.Data.Model.Directory;

namespace SonOfPicasso.Core.Tests.Services
{
    public class ImageManagementServiceTests : UnitTestsBase, IDisposable
    {
        public ImageManagementServiceTests(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        private static readonly Faker<Directory> FakeNewDirectory
            = new AutoFaker<Directory>().RuleFor(directory1 => directory1.Id, 0)
                .RuleFor(directory1 => directory1.Images, (List<Image>) null)
                .RuleFor(directory1 => directory1.Path, faker => faker.System.DirectoryPathWindows());

        private static readonly Faker<ExifData> FakeNewExifData
            = new AutoFaker<ExifData>().RuleFor(exifData => exifData.Id, 0);

        [Fact]
        public void AddImageToAlbum()
        {
            var directoryPath = Faker.System.DirectoryPathWindows();
            var directory = new Directory
            {
                Id = Faker.Random.Int(1),
                Path = directoryPath,
                Images = new List<Image>()
            };

            var images = Faker.Make(Faker.Random.Int(3, 5), () => new Image
            {
                Path = Path.Join(directoryPath) + Faker.System.FileName("jpg"),
                DirectoryId = directory.Id
            });

            directory.Images.AddRange(images);

            var unitOfWork = Substitute.For<IUnitOfWork>();
            unitOfWork.DirectoryRepository.Get()
                .ReturnsForAnyArgs(new[] {directory, FakeNewDirectory});

            UnitOfWorkQueue.Enqueue(unitOfWork);

            var imageManagementService = AutoSubstitute.Resolve<ImageManagementService>();

            var albumName = Faker.Random.Words(2);
            var albumId = Faker.Random.Int(1);

            unitOfWork.AlbumRepository.Get().ReturnsForAnyArgs(new[] {new Album {Id = albumId, Name = albumName}});

            unitOfWork.ImageRepository.GetById(Arg.Any<int>())
                .ReturnsForAnyArgs(info => { return images.First(i => i.Id == (int) info.Arg<object>()); });

            imageManagementService.AddImagesToAlbum(albumId, images.Select(image => image.Id))
                .Subscribe(unit => AutoResetEvent.Set());

            TestSchedulerProvider.TaskPool.AdvanceBy(1);

            AutoResetEvent.WaitOne(10).Should().BeTrue();

            unitOfWork.AlbumImageRepository.ReceivedWithAnyArgs(images.Count)
                .Insert(null);

            unitOfWork.Received(1).Save();

            unitOfWork.Received(1).Dispose();
        }

        [Fact]
        public void ShouldScanFolderWhenDirectoryModelExists()
        {
            var directoryPath = Faker.System.DirectoryPathWindows();
            var imagePath = Path.Join(directoryPath, Faker.System.FileName("jpg"));

            var directory = new Directory
            {
                Id = Faker.Random.Int(1),
                Path = directoryPath,
                Images = new List<Image>()
            };

            var unitOfWork = Substitute.For<IUnitOfWork>();
            unitOfWork.DirectoryRepository.Get()
                .ReturnsForAnyArgs(new[] {directory, FakeNewDirectory});

            UnitOfWorkQueue.Enqueue(unitOfWork);

            MockFileSystem.AddDirectory(directoryPath);
            MockFileSystem.AddFile(imagePath, new MockFileData(new byte[0]));

            AutoSubstitute.Resolve<IImageLocationService>()
                .GetImages(Arg.Any<string>())
                .Returns(Observable.Return(new[] {imagePath}));

            var newExifData = (ExifData) FakeNewExifData;
            AutoSubstitute.Resolve<IExifDataService>().GetExifData(imagePath)
                .Returns(Observable.Return(newExifData));

            var imageManagementService = AutoSubstitute.Resolve<ImageManagementService>();
            imageManagementService.ScanFolder(directoryPath)
                .Subscribe(unit => AutoResetEvent.Set());

            TestSchedulerProvider.TaskPool.AdvanceBy(1);

            AutoResetEvent.WaitOne(10).Should().BeTrue();

            unitOfWork.ImageRepository.DidNotReceive().Insert(Arg.Any<Image>());

            directory.Images.Count.Should().Be(1);

            using (var assertionScope = new AssertionScope())
            {
                var image = directory.Images.First();
                image.Path.Should().Be(imagePath);
                image.ExifData.Should().Be(newExifData);
            }

            unitOfWork.Received(1)
                .Save();

            unitOfWork.Received(1)
                .Dispose();
        }
    }
}