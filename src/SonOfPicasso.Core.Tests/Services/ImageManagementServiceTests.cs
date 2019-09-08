using System;
using System.Collections.Generic;
using System.IO;
using Autofac.Extras.NSubstitute;
using NSubstitute;
using SonOfPicasso.Data.Model;
using SonOfPicasso.Data.Repository;
using SonOfPicasso.Testing.Common;
using SonOfPicasso.Testing.Common.Extensions;
using Xunit;
using Xunit.Abstractions;
using Directory = SonOfPicasso.Data.Model.Directory;

namespace SonOfPicasso.Core.Tests.Services
{
    public class ImageManagementServiceTests : TestsBase, IDisposable
    {
        private readonly AutoSubstitute _autoSubstitute;
        private readonly Queue<IUnitOfWork> _unitOfWorkQueue;

        public ImageManagementServiceTests(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
            _autoSubstitute = new AutoSubstitute();

            _unitOfWorkQueue = new Queue<IUnitOfWork>();
            _autoSubstitute.Provide<Func<IUnitOfWork>>(() => _unitOfWorkQueue.Dequeue());
        }

        [Fact]
        public void ShouldAddDirectory()
        {
            var directoryPath = Faker.System.DirectoryPathWindows();

            var unitOfWork = Substitute.For<IUnitOfWork>();
            unitOfWork.DirectoryRepository.Get().Returns(new Directory[0]);

            _unitOfWorkQueue.Enqueue(unitOfWork);

            var imageManagementService = _autoSubstitute.Resolve<Core.Services.ImageManagementService>();
            imageManagementService.AddFolder(directoryPath);

            unitOfWork.DirectoryRepository
                .ReceivedWithAnyArgs(1)
                .Insert(Arg.Any<Directory>());

            unitOfWork.Received(1)
                .Save();

            unitOfWork.Received(1)
                .Dispose();
        }

        [Fact]
        public void ShouldSkipAddingDirectoryIfExists()
        {
            var directoryPath = Faker.System.DirectoryPathWindows();

            var unitOfWork = Substitute.For<IUnitOfWork>();
            unitOfWork.DirectoryRepository.Get().Returns(new Directory[1]{new Directory()
            {
                Id = Faker.Random.Int(1),
                Path = directoryPath,
                Images = new List<Image>()
            }});

            _unitOfWorkQueue.Enqueue(unitOfWork);

            var imageManagementService = _autoSubstitute.Resolve<Core.Services.ImageManagementService>();
            imageManagementService.AddFolder(directoryPath);

            unitOfWork.DirectoryRepository
                .DidNotReceiveWithAnyArgs()
                .Insert(Arg.Any<Directory>());

            unitOfWork.DidNotReceive()
                .Save();

            unitOfWork.Received(1)
                .Dispose();
        }

        [Fact]
        public void ShouldSkipAddingDirectoryIfChildOfExisting()
        {
            var directoryPath = Faker.System.DirectoryPathWindows();
            var parentDirectoryPath = new DirectoryInfo(directoryPath).Parent.ToString();

            var unitOfWork = Substitute.For<IUnitOfWork>();
            unitOfWork.DirectoryRepository.Get().Returns(new Directory[1]{new Directory()
            {
                Id = Faker.Random.Int(1),
                Path = parentDirectoryPath,
                Images = new List<Image>()
            }});

            _unitOfWorkQueue.Enqueue(unitOfWork);

            var imageManagementService = _autoSubstitute.Resolve<Core.Services.ImageManagementService>();
            imageManagementService.AddFolder(directoryPath);

            unitOfWork.DirectoryRepository
                .DidNotReceiveWithAnyArgs()
                .Insert(Arg.Any<Directory>());

            unitOfWork.DidNotReceive()
                .Save();

            unitOfWork.Received(1)
                .Dispose();
        }

        public void Dispose()
        {
            _autoSubstitute.Dispose();
        }
    }
}