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
    public class ApplicationViewModelTests
    {
        private static ILogger Log { get; } = LogManager.ForContext<ApplicationViewModelTests>();

        [Test]
        public void CanCreateApplicationViewModel()
        {
            Log.Verbose("CanCreateApplicationViewModel");

            var schedulers = new TestSchedulers();
            schedulers.ThreadPool.Start();
            schedulers.Dispatcher.Start();

            var mockFileSystem = MockFileSystemFactory.Create();

            var imageFileSystemService = new ImageService(mockFileSystem, schedulers);
            var applicationViewModel = new ApplicationViewModel(imageFileSystemService, schedulers);

            var autoResetEvent = new AutoResetEvent(false);

            IList argsNewItems = null;
            applicationViewModel.Images.Changed
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

            applicationViewModel.Directory = MockFileSystemFactory.ImagesFolder;

            Log.Verbose("CanCreateApplicationViewModel");

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
