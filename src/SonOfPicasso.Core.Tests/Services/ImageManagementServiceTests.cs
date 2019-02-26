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

            var folder = Faker.System.DirectoryPathWindows();

            Assert.Throws<ArgumentNullException>(() => imageLoadingService.AddFolder(null));
        }

        [Fact(Timeout = 1000)]
        public void CanAddFolder()
        {
            Logger.LogDebug("CanAddFolder");

            var sharedCache = Substitute.For<ISharedCache>();
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

            sharedCache.SetFolder(Arg.Any<ImageFolder>())
                .Returns(Observable.Return(Unit.Default));

            imageLocationService.GetImages(Arg.Any<string>())
                .Returns(Observable.Return(imagePaths));

            var imageLoadingService = this.CreateImageManagementService(
                sharedCache, imageLocationService);

            var autoResetEvent = new AutoResetEvent(false);

            imageLoadingService.AddFolder(imageFolderPath)
                .Subscribe(unit =>
                {
                    autoResetEvent.Set();
                });

            autoResetEvent.WaitOne();

            imageLocationService.Received().GetImages(imageFolderPath);

            sharedCache.Received().SetFolderList(Arg.Is<string[]>(strings => 
                strings.OrderBy(s => s)
                    .SequenceEqual(testPaths.OrderBy(s => s))));

            sharedCache.Received().SetFolder(Arg.Is<ImageFolder>(folder => 
                folder.Path == imageFolderPath && 
                folder.Images.OrderBy(s => s)
                    .SequenceEqual(imagePaths.OrderBy(s => s))));
        }
    }
}