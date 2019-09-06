using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using Autofac.Extras.NSubstitute;
using Bogus.Extensions;
using FluentAssertions;
using NSubstitute;
using SonOfPicasso.Core.Interfaces;
using SonOfPicasso.Core.Models;
using SonOfPicasso.Core.Services;
using SonOfPicasso.Testing.Common;
using SonOfPicasso.Testing.Common.Extensions;
using SonOfPicasso.Testing.Common.Services;
using Xunit;
using Xunit.Abstractions;

namespace SonOfPicasso.Core.Tests.Services
{
    public class ImageManagementServiceTests : TestsBase
    {
        public ImageManagementServiceTests(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        [Fact]
        public void CantAddNullFolder()
        {
            Logger.Debug("CantAddNullFolder");
            using (var autoSub = new AutoSubstitute())
            {
                var imageManagementService = autoSub.Resolve<ImageManagementService>();

                var folder = Faker.System.DirectoryPathWindows();

                Assert.Throws<ArgumentNullException>(() => imageManagementService.AddFolder(null));
            }
        }

        [Fact]
        public void CantRemoveNullFolder()
        {
            Logger.Debug("CantRemoveNullFolder");
            using (var autoSub = new AutoSubstitute())
            {
                var imageManagementService = autoSub.Resolve<ImageManagementService>();

                var folder = Faker.System.DirectoryPathWindows();

                Assert.Throws<ArgumentNullException>(() => imageManagementService.AddFolder(null));
            }
        }

        [Fact]
        public void CantRemoveFolderThatDoesntExist()
        {
            Logger.Debug("CantRemoveFolderThatDoesntExist");
            using (var autoSub = new AutoSubstitute())
            {
                var imageManagementService = autoSub.Resolve<ImageManagementService>();

                var folder = Faker.System.DirectoryPathWindows();
                SonOfPicassoException exception = null;

                var autoResetEvent = new AutoResetEvent(false);

                imageManagementService.RemoveFolder(folder)
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
        }

        [Fact(Timeout = 1000)]
        public void CanRemoveFolder()
        {
            Logger.Debug("CanRemoveFolder");
            using (var autoSub = new AutoSubstitute())
            {
                var dataCache = autoSub.Resolve<IDataCache>();
                var testPaths = Faker.Make(5, Faker.System.DirectoryPathWindows)
                    .Distinct()
                    .ToArray();

                var imageFolderPath = testPaths.First();
                var remainingPaths = testPaths.Skip(1).ToArray();

                dataCache.GetFolderList()
                    .Returns(Observable.Return(testPaths));

                dataCache.SetFolderList(Arg.Any<string[]>())
                    .Returns(Observable.Return(Unit.Default));

                dataCache.DeleteFolder(Arg.Any<string>())
                    .Returns(Observable.Return(Unit.Default));

                var imageManagementService = autoSub.Resolve<ImageManagementService>();

                var autoResetEvent = new AutoResetEvent(false);

                imageManagementService.RemoveFolder(imageFolderPath)
                    .Subscribe(_ => autoResetEvent.Set());

                autoResetEvent.WaitOne();

                dataCache.Received().SetFolderList(Arg.Is<string[]>(strings =>
                    strings.SequenceEqual(remainingPaths)));

                dataCache.Received().DeleteFolder(imageFolderPath);
            }
        }

        [Fact(Timeout = 1000)]
        public void CanAddFolder()
        {
            Logger.Debug("CanAddFolder");
            using (var autoSub = new AutoSubstitute())
            {
                var dataCache = autoSub.Resolve<IDataCache>();
                var imageLocationService = autoSub.Resolve<IImageLocationService>();

                var testPaths = Faker.Make(5, Faker.System.DirectoryPathWindows)
                    .Distinct()
                    .ToArray();

                var imageFolderPath = testPaths.First();

                var imagePaths = Faker.Make(5, () => Faker.System.FileName("png"))
                    .Select(s => Path.Combine(imageFolderPath, s))
                    .ToArray();

                dataCache.GetFolderList()
                    .Returns(Observable.Return(testPaths.Skip(1).ToArray()));

                dataCache.SetFolderList(Arg.Any<string[]>())
                    .Returns(Observable.Return(Unit.Default));

                dataCache.SetFolder(Arg.Any<ImageFolderModel>())
                    .Returns(Observable.Return(Unit.Default));

                dataCache.SetImage(Arg.Any<ImageModel>())
                    .Returns(Observable.Return(Unit.Default));

                imageLocationService.GetImages(Arg.Any<string>())
                    .Returns(Observable.Return(imagePaths));

                var imageManagementService = autoSub.Resolve<ImageManagementService>();

                var autoResetEvent = new AutoResetEvent(false);

                ImageFolderModel imageFolderModel = null;
                ImageModel[] imageModels = null;

                imageManagementService.AddFolder(imageFolderPath)
                    .Subscribe(tuple =>
                    {
                        (imageFolderModel, imageModels) = tuple;
                        autoResetEvent.Set();
                    });

                autoResetEvent.WaitOne();

                imageLocationService.Received().GetImages(imageFolderPath);

                dataCache.Received().SetFolderList(Arg.Is<string[]>(strings =>
                    strings.OrderBy(s => s)
                        .SequenceEqual(testPaths.OrderBy(s => s))));

                dataCache.Received().SetFolder(Arg.Is<ImageFolderModel>(folder =>
                    folder.Path == imageFolderPath &&
                    folder.Images.OrderBy(s => s)
                        .SequenceEqual(imagePaths.OrderBy(s => s))));

                dataCache.Received(5).SetImage(Arg.Any<ImageModel>());

                dataCache.ReceivedCalls()
                    .Where(call => call.GetMethodInfo().Name == "SetImage")
                    .Select(call => (ImageModel)call.GetArguments()[0])
                    .Select(model => model.Path)
                    .Should()
                    .BeEquivalentTo(imagePaths);

                imageFolderModel.Should().NotBeNull();
                imageFolderModel.Path.Should().Be(imageFolderPath);

                imageModels.Select(model => model.Path)
                    .Should().BeEquivalentTo(imagePaths);
            }
        }

        [Fact(Timeout = 1000)]
        public void CantAddFolderASecondTime()
        {
            Logger.Debug("CantAddFolderASecondTime");
            using (var autoSub = new AutoSubstitute())
            {
                var dataCache = autoSub.Resolve<IDataCache>();
                var imageLocationService = autoSub.Resolve<IImageLocationService>();


                var testPaths = Faker.Make(5, Faker.System.DirectoryPathWindows)
                    .Distinct()
                    .ToArray();

                var imageFolderPath = testPaths.First();

                var imagePaths = Faker.Make(5, () => Faker.System.FileName("png"))
                    .Select(s => Path.Combine(imageFolderPath, s))
                    .ToArray();

                dataCache.GetFolderList()
                    .Returns(Observable.Return(testPaths));

                dataCache.SetFolderList(Arg.Any<string[]>())
                    .Returns(Observable.Return(Unit.Default));

                dataCache.SetFolder(Arg.Any<ImageFolderModel>())
                    .Returns(Observable.Return(Unit.Default));

                imageLocationService.GetImages(Arg.Any<string>())
                    .Returns(Observable.Return(imagePaths));

                var imageManagementService = autoSub.Resolve<ImageManagementService>();

                var autoResetEvent = new AutoResetEvent(false);

                SonOfPicassoException exception = null;

                imageManagementService.AddFolder(imageFolderPath)
                    .Subscribe(
                        _ => { },
                        ex =>
                        {
                            exception = ex as SonOfPicassoException;
                            autoResetEvent.Set();
                        });

                autoResetEvent.WaitOne();

                imageLocationService.DidNotReceiveWithAnyArgs().GetImages(Arg.Any<string>());
                dataCache.DidNotReceiveWithAnyArgs().SetFolderList(Arg.Any<string[]>());
                dataCache.DidNotReceiveWithAnyArgs().SetFolder(Arg.Any<ImageFolderModel>());

                exception.Should().NotBeNull();
                exception.Message.Should().Be("Folder already exists");
            }
        }

        [Fact(Timeout = 1000)]
        public void CanGetImageFolders()
        {
            Logger.Debug("CanGetImageFolders");
            using (var autoSub = new AutoSubstitute())
            {
                var dataCache = autoSub.Resolve<IDataCache>();

                var testPaths = Faker.Make(5, Faker.System.DirectoryPathWindows)
                    .Distinct()
                    .ToArray();

                var imageFolders = testPaths
                    .Select(s => new ImageFolderModel { Path = s })
                    .ToArray();

                var imageFolderDictionary = imageFolders
                    .ToDictionary(s => s.Path);

                dataCache.GetFolderList()
                    .Returns(Observable.Return(testPaths.ToArray()));

                dataCache.GetFolder(Arg.Any<string>())
                    .Returns(info => Observable.Return(imageFolderDictionary[info.Arg<string>()]));

                var imageManagementService = autoSub.Resolve<ImageManagementService>();

                var autoResetEvent = new AutoResetEvent(false);

                var results = new List<ImageFolderModel>();

                imageManagementService.GetAllImageFolders()
                    .Subscribe(
                        imageFolder => results.Add(imageFolder),
                        () => autoResetEvent.Set());

                autoResetEvent.WaitOne();

                results.Should().BeEquivalentTo(imageFolders);
            }
        }

        [Fact(Timeout = 1000)]
        public void CanGetImages()
        {
            Logger.Debug("CanGetImages");
            using (var autoSub = new AutoSubstitute())
            {
                var dataCache = autoSub.Resolve<IDataCache>();

                var testPaths = Faker.Make(5, Faker.System.DirectoryPathWindows)
                    .Distinct()
                    .ToArray();

                var imagePathsByPath = testPaths
                    .ToDictionary(
                        testPath => testPath,
                        testPath => Faker
                            .Make(5, () => Path.Combine(testPath, Faker.System.FileName("png")))
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

                dataCache.GetFolderList()
                    .Returns(Observable.Return(testPaths.ToArray()));

                dataCache.GetFolder(Arg.Any<string>())
                    .Returns(info => Observable.Return(imageFoldersByPath[info.Arg<string>()]));

                dataCache.GetImage(Arg.Any<string>())
                    .Returns(info => Observable.Return(imagesByPath[info.Arg<string>()]));

                var imageManagementService = autoSub.Resolve<ImageManagementService>();

                var autoResetEvent = new AutoResetEvent(false);

                var results = new List<ImageModel>();

                imageManagementService.GetAllImages()
                    .Subscribe(
                        image => results.Add(image),
                        () => autoResetEvent.Set());

                autoResetEvent.WaitOne();

                results.Should().BeEquivalentTo(images);
            }
        }
    }
}