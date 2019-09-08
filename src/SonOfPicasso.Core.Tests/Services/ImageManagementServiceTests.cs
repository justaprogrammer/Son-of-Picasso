using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive.Linq;
using System.Threading;
using Autofac.Extras.NSubstitute;
using FluentAssertions;
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
    public class ImageManagementServiceTests : TestsBase, IDisposable
    {
        public ImageManagementServiceTests(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
            _autoSubstitute = new AutoSubstitute();

            _testSchedulerProvider = new TestSchedulerProvider();
            _autoSubstitute.Provide<ISchedulerProvider>(_testSchedulerProvider);

            _unitOfWorkQueue = new Queue<IUnitOfWork>();
            _autoSubstitute.Provide<Func<IUnitOfWork>>(() => _unitOfWorkQueue.Dequeue());

            _mockFileSystem = new MockFileSystem();
            _autoSubstitute.Provide<IFileSystem>(_mockFileSystem);
        }

        public void Dispose()
        {
            _autoSubstitute.Dispose();
        }

        private readonly AutoSubstitute _autoSubstitute;
        private readonly Queue<IUnitOfWork> _unitOfWorkQueue;
        private readonly MockFileSystem _mockFileSystem;
        private readonly TestSchedulerProvider _testSchedulerProvider;

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
                .ReturnsForAnyArgs(new[] {directory, FakerProfiles.FakeNewDirectory});

            _unitOfWorkQueue.Enqueue(unitOfWork);

            var autoResetEvent = new AutoResetEvent(false);

            var imageManagementService = _autoSubstitute.Resolve<ImageManagementService>();

            var albumName = Faker.Random.Words(2);
            var albumId = Faker.Random.Int(1);

            unitOfWork.AlbumRepository.Get().ReturnsForAnyArgs(new[] {new Album {Id = albumId, Name = albumName}});

            unitOfWork.ImageRepository.GetById(Arg.Any<int>())
                .ReturnsForAnyArgs(info =>
                {
                    return images.First(i => i.Id == (int) info.Arg<object>());
                });

            imageManagementService.AddImagesToAlbum(albumId, images.Select(image => image.Id))
                .Subscribe(unit => autoResetEvent.Set());

            _testSchedulerProvider.TaskPool.AdvanceBy(1);

            autoResetEvent.WaitOne(10).Should().BeTrue();

            unitOfWork.AlbumImageRepository.ReceivedWithAnyArgs(images.Count)
                .Insert(null);

            unitOfWork.Received(1).Save();

            unitOfWork.Received(1).Dispose();
        }

        [Fact]
        public void ScanFolder()
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
                .ReturnsForAnyArgs(new[] {directory, FakerProfiles.FakeNewDirectory});

            _unitOfWorkQueue.Enqueue(unitOfWork);

            _mockFileSystem.AddDirectory(directoryPath);
            _mockFileSystem.AddFile(imagePath, new MockFileData(new byte[0]));

            var autoResetEvent = new AutoResetEvent(false);

            _autoSubstitute.Resolve<IImageLocationService>()
                .GetImages(Arg.Any<string>())
                .Returns(Observable.Return(new[] {imagePath}));

            var imageManagementService = _autoSubstitute.Resolve<ImageManagementService>();
            imageManagementService.ScanFolder(directoryPath)
                .Subscribe(unit => autoResetEvent.Set());

            _testSchedulerProvider.TaskPool.AdvanceBy(1);

            autoResetEvent.WaitOne(10).Should().BeTrue();

            unitOfWork.ImageRepository.Received(1)
                .Insert(Arg.Any<Image>());

            unitOfWork.Received(1)
                .Save();

            unitOfWork.Received(1)
                .Dispose();

            directory.Images.Count.Should().Be(1);
        }
    }
}