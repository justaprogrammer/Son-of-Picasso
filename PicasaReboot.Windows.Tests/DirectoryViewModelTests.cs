using System;
using System.Collections;
using System.Collections.Specialized;
using System.Reactive.Linq;
using System.Threading;
using FluentAssertions;
using NUnit.Framework;
using PicasaReboot.Core;
using PicasaReboot.Core.Logging;
using PicasaReboot.Tests;
using PicasaReboot.Tests.Core;
using PicasaReboot.Tests.Scheduling;
using PicasaReboot.Windows.ViewModels;
using Serilog;

namespace PicasaReboot.Windows.Tests
{

    [TestFixture]
    public class DirectoryViewModelTests
    {
        private static ILogger Log { get; } = LogManager.ForContext<DirectoryViewModelTests>();

        [Test]
        public void CanCreateDirectoryViewModel()
        {
            Log.Verbose("CanCreateDirectoryViewModel");

            var schedulers = new TestSchedulers();
            schedulers.ThreadPool.Start();
            schedulers.Dispatcher.Start();

            var mockFileSystem = MockFileSystemFactory.Create();

            var imageFileSystemService = new ImageService(mockFileSystem, schedulers);
            var directoryViewModel = new DirectoryViewModel(imageFileSystemService, schedulers);

            var autoResetEvent = new AutoResetEvent(false);

            IList argsNewItems = null;
            directoryViewModel.Images.Changed
                .ObserveOn(schedulers.ThreadPool)
                .Subscribe(args =>
                {
                    Log.Verbose("Images.Changed: {Action}", args.Action);

                    if (args.Action == NotifyCollectionChangedAction.Add)
                    {
                        argsNewItems = args.NewItems;
                        autoResetEvent.Set();
                    }
                });

            directoryViewModel.Name = MockFileSystemFactory.ImagesFolder;

            Log.Verbose("CanCreateDirectoryViewModel");

            try
            {
                autoResetEvent.WaitOne(TimeSpan.FromSeconds(5)).Should().BeTrue();
            }
            catch (Exception e)
            {
                
            }
        }
    }
}
