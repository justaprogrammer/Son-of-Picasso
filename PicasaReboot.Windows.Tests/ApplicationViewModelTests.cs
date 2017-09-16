using System;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using FluentAssertions;
using NUnit.Framework;
using PicasaReboot.Core;
using PicasaReboot.Tests;
using PicasaReboot.Windows.ViewModels;

namespace PicasaReboot.Windows.Tests
{
    [TestFixture]
    public class ApplicationViewModelTests
    {
        [Test]
        public void CanCreateImageViewModel()
        {
            var mockFileSystem = MockFileSystemFactory.Create();

            var imageFileSystemService = new ImageService(mockFileSystem);
            var applicationViewModel = new ApplicationViewModel(imageFileSystemService, MockFileSystemFactory.ImagesFolder);

            var autoResetEvent = new AutoResetEvent(false);

            ImageViewModel imageViewModel = null;

            applicationViewModel.Images.Subscribe(Observer.Create<ImageViewModel>(model =>
                {
                    imageViewModel = model;
                },
                () =>
                {
                    autoResetEvent.Set();
                }));

            autoResetEvent.WaitOne();
            imageViewModel.Should().NotBeNull();
        }
    }
}
