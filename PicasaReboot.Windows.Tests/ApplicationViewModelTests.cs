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
}
