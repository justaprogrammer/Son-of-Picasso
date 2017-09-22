using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Threading;
using FluentAssertions;
using NUnit.Framework;
using PicasaReboot.Core;
using PicasaReboot.Tests;
using PicasaReboot.Windows.ViewModels;
using ReactiveUI;
using Serilog;

namespace PicasaReboot.Windows.Tests
{
    [TestFixture]
    public class ApplicationViewModelTests
    {
        private static ILogger Log { get; } = LogManager.ForContext<ApplicationViewModelTests>();

        [Test]
        public void CanCreateApplicationViewModel()
        {
            var mockFileSystem = MockFileSystemFactory.Create();

            var imageFileSystemService = new ImageService(mockFileSystem);
            var applicationViewModel = new ApplicationViewModel(imageFileSystemService);

            var autoResetEvent = new AutoResetEvent(false);

            IList argsNewItems = null;
            applicationViewModel.Images.Changed.Subscribe(args =>
            {
                if (args.Action == NotifyCollectionChangedAction.Add)
                {
                    argsNewItems = args.NewItems;
                    autoResetEvent.Set();
                }
            });

            applicationViewModel.Directory = MockFileSystemFactory.ImagesFolder;

            autoResetEvent.WaitOne();

            argsNewItems.Should().NotBeNull();
        }
    }

    [TestFixture]
    public class ImageViewModelTests
    {
        private static ILogger Log { get; } = LogManager.ForContext<ImageViewModelTests>();

        [Test]
        public void CanCreateImageViewModel()
        {
            var mockFileSystem = MockFileSystemFactory.Create();

            var imageFileSystemService = new ImageService(mockFileSystem);
            var imageViewModel = new ImageViewModel(imageFileSystemService, MockFileSystemFactory.Image1Jpg);

            imageViewModel.File.Should().Be(MockFileSystemFactory.Image1Jpg);
            imageViewModel.Image.Should().NotBeNull();
        }
    }
}
