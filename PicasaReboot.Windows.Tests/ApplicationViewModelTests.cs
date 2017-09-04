using System;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using FluentAssertions;
using NUnit.Framework;
using PicasaReboot.Core;
using PicasaReboot.Core.Extensions;
using PicasaReboot.SampleImages;
using PicasaReboot.Windows.ViewModels;

namespace PicasaReboot.Windows.Tests
{
    [TestFixture]
    public class ApplicationViewModelTests
    {
        [Test]
        public void TestOne()
        {
            Log.Information("TestOne");

            var image1Bytes = Resources.image1.GetBytes();

            var image1Jpg = @"c:\images\image1.jpg";
            var imageFolder = @"c:\images";

            var mockFileSystem = new MockFileSystem();
            mockFileSystem.AddDirectory(imageFolder);
            mockFileSystem.AddFile(image1Jpg, new MockFileData(image1Bytes));

            var imageFileSystemService = new ImageService(mockFileSystem);
            var applicationViewModel = new ApplicationViewModel(imageFileSystemService, imageFolder);
            //var observeOn = Observable.ObserveOn(Observable.Create(
            //  observer => { }), ThreadPoolScheduler.Instance);

            var observeOn = Observer.Create<ImageViewModel>(model =>
            {
                Log.Information("Get Model");
            });
            var imageViewModels = applicationViewModel.Images.Subscribe(observeOn, ThreadPoolScheduler.Instance);
        }
    }
}
