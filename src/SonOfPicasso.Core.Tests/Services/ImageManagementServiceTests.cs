using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using Bogus.Extensions;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SonOfPicasso.Core.Interfaces;
using SonOfPicasso.Core.Models;
using SonOfPicasso.Core.Services;
using SonOfPicasso.Core.Tests.Extensions;
using SonOfPicasso.Testing.Common;
using SonOfPicasso.Testing.Common.Extensions;
using SonOfPicasso.Testing.Common.Services;
using Xunit;
using Xunit.Abstractions;

namespace SonOfPicasso.Core.Tests.Services
{
    public class ImageManagementServiceTests : TestsBase<ImageManagementServiceTests>
    {
        public ImageManagementServiceTests(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        [Fact]
        public void CanInitialize()
        {
            Logger.LogDebug("CanInitialize");
            var imageLoadingService = this.CreateImageManagementService();
        }

        [Fact]
        public void CantAddNullFolder()
        {
            Logger.LogDebug("CantAddNullFolder");

            var imageLoadingService = this.CreateImageManagementService();

            var folder = Faker.System.DirectoryPathWindows();

            Assert.Throws<ArgumentNullException>(() => imageLoadingService.AddFolder(null));
        }

        [Fact]
        public void CantRemoveNullFolder()
        {
            Logger.LogDebug("CantRemoveNullFolder");

            var imageLoadingService = this.CreateImageManagementService();

            Assert.Throws<ArgumentNullException>(() => imageLoadingService.AddFolder(null));
        }

        [Fact]
        public void CantRemoveFolderThatDoesntExist()
        {
            Logger.LogDebug("CantRemoveFolderThatDoesntExist");

            var imageLoadingService = this.CreateImageManagementService();

            var folder = Faker.System.DirectoryPathWindows();
            SonOfPicassoException exception = null;

            var autoResetEvent = new AutoResetEvent(false);

            imageLoadingService.RemoveFolder(folder)
                .Subscribe(_ => { },
                    ex =>
                    {
                        exception = ex as SonOfPicassoException;
                        autoResetEvent.Set();
                    });

            autoResetEvent.WaitOne();

            exception.Should().NotBeNull();
            exception.Message.Should().Be("Folder does not exist");
        }

        [Fact(Timeout = 1000)]
        public void CanRemoveFolder()
        {
            Logger.LogDebug("CanRemoveFolder");

            var sharedCache = Substitute.For<IDataCache>();

            var testPaths = Faker.Make(5, Faker.System.DirectoryPathWindows)
                .Distinct()
                .ToArray();

            var imageFolderPath = testPaths.First();
            var remainingPaths = testPaths.Skip(1).ToArray();

            sharedCache.GetFolderList()
                .Returns(Observable.Return(testPaths));

            sharedCache.SetFolderList(Arg.Any<string[]>())
                .Returns(Observable.Return(Unit.Default));

            sharedCache.DeleteFolder(Arg.Any<string>())
                .Returns(Observable.Return(Unit.Default));

            var imageLoadingService = this.CreateImageManagementService(sharedCache);

            var autoResetEvent = new AutoResetEvent(false);

            imageLoadingService.RemoveFolder(imageFolderPath)
                .Subscribe(_ => autoResetEvent.Set());

            autoResetEvent.WaitOne();

            sharedCache.Received().SetFolderList(Arg.Is<string[]>(strings =>
                strings.SequenceEqual(remainingPaths)));

            sharedCache.Received().DeleteFolder(imageFolderPath);
        }

        [Fact(Timeout = 1000)]
        public void CanAddFolder()
        {
            Logger.LogDebug("CanAddFolder");

            var sharedCache = Substitute.For<IDataCache>();
            var imageLocationService = Substitute.For<IImageLocationService>();

            var testPaths = Faker.Make(5, Faker.System.DirectoryPathWindows)
                .Distinct()
                .ToArray();

            var imageFolderPath = testPaths.First();

            var imagePaths = Faker.Make(5, () => Faker.System.FileName("png"))
                .Select(s => Path.Join(imageFolderPath, s))
                .ToArray();

            sharedCache.GetFolderList()
                .Returns(Observable.Return(testPaths.Skip(1).ToArray()));

            sharedCache.SetFolderList(Arg.Any<string[]>())
                .Returns(Observable.Return(Unit.Default));

            sharedCache.SetFolder(Arg.Any<ImageFolderModel>())
                .Returns(Observable.Return(Unit.Default));

            sharedCache.SetImage(Arg.Any<ImageModel>())
                .Returns(Observable.Return(Unit.Default));

            imageLocationService.GetImages(Arg.Any<string>())
                .Returns(Observable.Return(imagePaths));

            var imageLoadingService = this.CreateImageManagementService(
                sharedCache, imageLocationService);

            var autoResetEvent = new AutoResetEvent(false);

            ImageFolderModel imageFolderModel = null;
            ImageModel[] imageModels = null;

            imageLoadingService.AddFolder(imageFolderPath)
                .Subscribe(tuple =>
                {
                    (imageFolderModel, imageModels) = tuple;
                    autoResetEvent.Set();
                });

            autoResetEvent.WaitOne();

            imageLocationService.Received().GetImages(imageFolderPath);

            sharedCache.Received().SetFolderList(Arg.Is<string[]>(strings =>
                strings.OrderBy(s => s)
                    .SequenceEqual(testPaths.OrderBy(s => s))));

            sharedCache.Received().SetFolder(Arg.Is<ImageFolderModel>(folder =>
                folder.Path == imageFolderPath &&
                folder.Images.OrderBy(s => s)
                    .SequenceEqual(imagePaths.OrderBy(s => s))));

            sharedCache.Received(5).SetImage(Arg.Any<ImageModel>());

            sharedCache.ReceivedCalls()
                .Where(call => call.GetMethodInfo().Name == "SetImage")
                .Select(call => (ImageModel) call.GetArguments()[0])
                .Select(model => model.Path)
                .Should()
                .BeEquivalentTo(imagePaths);

            imageFolderModel.Should().NotBeNull();
            imageFolderModel.Path.Should().Be(imageFolderPath);

            imageModels.Select(model => model.Path)
                .Should().BeEquivalentTo(imagePaths);
        }

        [Fact(Timeout = 1000)]
        public void CantAddFolderASecondTime()
        {
            Logger.LogDebug("CantAddFolderASecondTime");

            var sharedCache = Substitute.For<IDataCache>();
            var imageLocationService = Substitute.For<IImageLocationService>();

            var testPaths = Faker.Make(5, Faker.System.DirectoryPathWindows)
                .Distinct()
                .ToArray();

            var imageFolderPath = testPaths.First();

            var imagePaths = Faker.Make(5, () => Faker.System.FileName("png"))
                .Select(s => Path.Join(imageFolderPath, s))
                .ToArray();

            sharedCache.GetFolderList()
                .Returns(Observable.Return(testPaths));

            sharedCache.SetFolderList(Arg.Any<string[]>())
                .Returns(Observable.Return(Unit.Default));

            sharedCache.SetFolder(Arg.Any<ImageFolderModel>())
                .Returns(Observable.Return(Unit.Default));

            imageLocationService.GetImages(Arg.Any<string>())
                .Returns(Observable.Return(imagePaths));

            var imageLoadingService = this.CreateImageManagementService(
                sharedCache, imageLocationService);

            var autoResetEvent = new AutoResetEvent(false);

            SonOfPicassoException exception = null;

            imageLoadingService.AddFolder(imageFolderPath)
                .Subscribe(
                    _ => { },
                    ex =>
                    {
                        exception = ex as SonOfPicassoException;
                        autoResetEvent.Set();
                    });

            autoResetEvent.WaitOne();

            imageLocationService.DidNotReceiveWithAnyArgs().GetImages(Arg.Any<string>());
            sharedCache.DidNotReceiveWithAnyArgs().SetFolderList(Arg.Any<string[]>());
            sharedCache.DidNotReceiveWithAnyArgs().SetFolder(Arg.Any<ImageFolderModel>());

            exception.Should().NotBeNull();
            exception.Message.Should().Be("Folder already exists");
        }

        [Fact(Timeout = 1000)]
        public void CanGetImageFolders()
        {
            Logger.LogDebug("CanGetImageFolders");

            var sharedCache = Substitute.For<IDataCache>();

            var testPaths = Faker.Make(5, Faker.System.DirectoryPathWindows)
                .Distinct()
                .ToArray();

            var imageFolders = testPaths
                .Select(s => new ImageFolderModel { Path = s })
                .ToArray();

            var imageFolderDictionary = imageFolders
                .ToDictionary(s => s.Path);

            sharedCache.GetFolderList()
                .Returns(Observable.Return(testPaths.ToArray()));

            sharedCache.GetFolder(Arg.Any<string>())
                .Returns(info => Observable.Return(imageFolderDictionary[info.Arg<string>()]));

            var imageLoadingService = this.CreateImageManagementService(
                sharedCache);

            var autoResetEvent = new AutoResetEvent(false);

            var results = new List<ImageFolderModel>();

            imageLoadingService.GetAllImageFolders()
                .Subscribe(
                    imageFolder => results.Add(imageFolder),
                    () => autoResetEvent.Set());

            autoResetEvent.WaitOne();

            results.Should().BeEquivalentTo(imageFolders);
        }

        [Fact(Timeout = 1000)]
        public void CanGetImages()
        {
            Logger.LogDebug("CanGetImages");

            var sharedCache = Substitute.For<IDataCache>();

            var testPaths = Faker.Make(5, Faker.System.DirectoryPathWindows)
                .Distinct()
                .ToArray();

            var imagePathsByPath = testPaths
                .ToDictionary(
                    testPath => testPath,
                    testPath => Faker
                        .Make(5, () => Path.Join(testPath, Faker.System.FileName("png")))
                        .ToArray());

            var images = imagePathsByPath
                .SelectMany(pair => pair.Value)
                .Select(imagePath => new ImageModel { Path = imagePath })
                .ToArray();

            var imagesByPath = images
                .ToDictionary(image => image.Path);

            var imageFolders = testPaths
                .Select(testPath => new ImageFolderModel
                {
                    Path = testPath,
                    Images = imagePathsByPath[testPath]
                })
                .ToArray();

            var imageFoldersByPath = imageFolders
                .ToDictionary(s => s.Path);

            sharedCache.GetFolderList()
                .Returns(Observable.Return(testPaths.ToArray()));

            sharedCache.GetFolder(Arg.Any<string>())
                .Returns(info => Observable.Return(imageFoldersByPath[info.Arg<string>()]));

            sharedCache.GetImage(Arg.Any<string>())
                .Returns(info => Observable.Return(imagesByPath[info.Arg<string>()]));

            var imageLoadingService = this.CreateImageManagementService(
                sharedCache);

            var autoResetEvent = new AutoResetEvent(false);

            var results = new List<ImageModel>();

            imageLoadingService.GetAllImages()
                .Subscribe(
                    image => results.Add(image),
                    () => autoResetEvent.Set());

            autoResetEvent.WaitOne();

            results.Should().BeEquivalentTo(images);
        }
    }
}