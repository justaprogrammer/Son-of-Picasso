using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Reactive.Linq;
using Bogus;
using FluentAssertions;
using FluentAssertions.Execution;
using NSubstitute;
using SonOfPicasso.Core.Interfaces;
using SonOfPicasso.Core.Services;
using SonOfPicasso.Data.Interfaces;
using SonOfPicasso.Data.Model;
using SonOfPicasso.Testing.Common;
using SonOfPicasso.Testing.Common.Extensions;
using Xunit;
using Xunit.Abstractions;

namespace SonOfPicasso.Core.Tests.Services
{
    public class ImageManagementServiceTests : UnitTestsBase, IDisposable
    {
        public ImageManagementServiceTests(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        private static readonly Faker<Folder> FakeNewFolder
            = new Faker<Folder>().RuleFor(directory1 => directory1.Id, 0)
                .RuleFor(directory1 => directory1.Images, (List<Image>) null)
                .RuleFor(directory1 => directory1.Path, faker => faker.System.DirectoryPathWindows());

        private static readonly Faker<ExifData> FakeNewExifData
            = new Faker<ExifData>().RuleFor(exifData => exifData.Id, 0);

        [Fact]
        public void ShouldScanFolderWhenDirectoryModelExists()
        {
            var directoryPath = Faker.System.DirectoryPathWindows();
            var imagePath = Path.Join(directoryPath, Faker.System.FileName("jpg"));

            var directory = new Folder
            {
                Id = Faker.Random.Int(1),
                Path = directoryPath,
                Images = new List<Image>()
            };

            var unitOfWork = Substitute.For<IUnitOfWork>();
            unitOfWork.FolderRepository.Get()
                .ReturnsForAnyArgs(new[] {directory, FakeNewFolder});

            UnitOfWorkQueue.Enqueue(unitOfWork);

            MockFileSystem.AddDirectory(directoryPath);
            MockFileSystem.AddFile(imagePath, new MockFileData(new byte[0]));

            AutoSubstitute.Resolve<IImageLocationService>()
                .GetImages(Arg.Any<string>())
                .Returns(Observable.Return(imagePath));

            var newExifData = (ExifData) FakeNewExifData;
            AutoSubstitute.Resolve<IExifDataService>().GetExifData(imagePath)
                .Returns(Observable.Return(newExifData));

            var imageManagementService = AutoSubstitute.Resolve<ImageManagementService>();
            imageManagementService.ScanFolder(directoryPath)
                .Subscribe(unit => { }, () => AutoResetEvent.Set());

            TestSchedulerProvider.TaskPool.AdvanceBy(1);

            AutoResetEvent.WaitOne(50).Should().BeTrue();

            unitOfWork.ImageRepository.DidNotReceive().Insert(Arg.Any<Image>());

            directory.Images.Count.Should().Be(1);

            using (new AssertionScope())
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