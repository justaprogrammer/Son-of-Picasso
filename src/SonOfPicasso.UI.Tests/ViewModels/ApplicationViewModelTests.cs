using System;
using System.Reactive.Linq;
using System.Threading;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SonOfPicasso.Core.Interfaces;
using SonOfPicasso.Testing.Common;
using SonOfPicasso.Testing.Common.Scheduling;
using SonOfPicasso.UI.Tests.Extensions;
using Xunit;
using Xunit.Abstractions;

namespace SonOfPicasso.UI.Tests.ViewModels
{
    public class ApplicationViewModelTests : TestsBase<ApplicationViewModelTests>
    {
        public ApplicationViewModelTests(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        [Fact(Timeout = 500)]
        public void CanInitialize()
        {
            Logger.LogDebug("CanInitialize");

            var testSchedulerProvider = new TestSchedulerProvider();
            var imageLocationService = Substitute.For<IImageLocationService>();
            var sharedCache = Substitute.For<ISharedCache>();

            sharedCache.GetFolderList()
                .Returns(Observable.Return(Array.Empty<string>()));

            var applicationViewModel = this.CreateApplicationViewModel(
                imageLocationService: imageLocationService,
                sharedCache: sharedCache,
                schedulerProvider: testSchedulerProvider);

            var autoResetEvent = new AutoResetEvent(false);

            applicationViewModel.Initialize()
                .Subscribe(_ => autoResetEvent.Set());

            sharedCache.Received().GetFolderList();

            testSchedulerProvider.MainThreadScheduler.AdvanceBy(1);
            testSchedulerProvider.TaskPool.AdvanceBy(1);
            testSchedulerProvider.TaskPool.AdvanceBy(1);

            autoResetEvent.WaitOne();
        }

        [Fact(Timeout = 500)]
        public void CanAddPath()
        {
            Logger.LogDebug("CanAddPath");

            var testSchedulerProvider = new TestSchedulerProvider();
            var imageLocationService = Substitute.For<IImageLocationService>();
            var sharedCache = Substitute.For<ISharedCache>();

            sharedCache.GetFolderList()
                .Returns(Observable.Return(Array.Empty<string>()));

            var applicationViewModel = this.CreateApplicationViewModel(
                imageLocationService: imageLocationService,
                sharedCache: sharedCache,
                schedulerProvider: testSchedulerProvider);

            var autoResetEvent = new AutoResetEvent(false);

            applicationViewModel.AddFolder.Execute(Faker.System.DirectoryPath())
                .Subscribe(unit => { autoResetEvent.Set(); });

            testSchedulerProvider.TaskPool.AdvanceBy(1);
            testSchedulerProvider.MainThreadScheduler.AdvanceBy(1);
            testSchedulerProvider.MainThreadScheduler.AdvanceBy(1);
            testSchedulerProvider.TaskPool.AdvanceBy(1);

            autoResetEvent.WaitOne();
        }
    }
}
