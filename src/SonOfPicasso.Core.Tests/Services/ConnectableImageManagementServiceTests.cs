using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using DynamicData;
using DynamicData.Binding;
using FluentAssertions;
using NSubstitute;
using SonOfPicasso.Core.Interfaces;
using SonOfPicasso.Core.Model;
using SonOfPicasso.Core.Services;
using SonOfPicasso.Data.Model;
using SonOfPicasso.Testing.Common;
using SonOfPicasso.Testing.Common.Extensions;
using Xunit;
using Xunit.Abstractions;

namespace SonOfPicasso.Core.Tests.Services
{
    public class ConnectableImageManagementServiceTests : UnitTestsBase
    {
        public ConnectableImageManagementServiceTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }

        [Fact]
        public void ShouldStart()
        {
            var folderWatcherService = AutoSubstitute.Resolve<IFolderWatcherService>();
            
            var folderRulesManagementService = AutoSubstitute.Resolve<IFolderRulesManagementService>();

            var currentFolderRules = Fakers.FolderRuleFaker
                .GenerateLazy(3)
                .ToList();

            folderRulesManagementService.GetFolderManagementRules()
                .ReturnsForAnyArgs(Observable.Return(currentFolderRules));

            var imageManagementService = AutoSubstitute.Resolve<IImageManagementService>();

            var imageContainer = Substitute.For<IImageContainer>();
            imageContainer.Id.Returns(Faker.Random.String());
            imageContainer.Date.Returns(Faker.Date.Recent());
            imageContainer.ContainerType.Returns(Faker.PickRandom<ImageContainerTypeEnum>());

            var returnThis = new[]
            {
                new ImageRef(Faker.Random.String(),
                    MockFileSystem.Path.Combine(Faker.System.DirectoryPathWindows(), Faker.System.FileName("png")),
                    Faker.Random.Int(1),
                    Faker.Date.Recent(),
                    imageContainer.Id,
                    imageContainer.ContainerType,
                    imageContainer.Date)
            };

            imageContainer.ImageRefs.Returns(returnThis);

            imageManagementService.GetAllImageContainers()
                .Returns(Observable.Return(imageContainer));

            var connectableImageManagementService = AutoSubstitute.Resolve<ConnectableImageManagementService>();

            var imageContainers = new ObservableCollectionExtended<IImageContainer>();

            connectableImageManagementService
                .ImageContainerCache
                .Connect()
                .Bind(imageContainers)
                .Subscribe(set =>
                {
                });

            var imageRefs = new ObservableCollectionExtended<ImageRef>();

            connectableImageManagementService
                .ImageRefCache
                .Connect()
                .Bind(imageRefs)
                .Subscribe(set =>
                {
                    AutoResetEvent.Set();
                });

            connectableImageManagementService
                .Start()
                .Subscribe(unit =>
                {
                    AutoResetEvent.Set();
                });

            WaitOne();

            imageContainers.Count.Should().Be(1);

            TestSchedulerProvider.TaskPool.AdvanceBy(1);
            WaitOne();

            imageRefs.Count.Should().Be(1);

            folderWatcherService.Received(1)
                .WatchFolders(currentFolderRules);
        }

        [Fact]
        public void ShouldScan()
        {
            var directoryPathWindows = Faker.System.DirectoryPathWindows();
         
            var folderRulesManagementService = AutoSubstitute.Resolve<IFolderRulesManagementService>();
            folderRulesManagementService.AddFolderManagementRule(default)
                .ReturnsForAnyArgs(Observable.Return(Unit.Default));

            var imageManagementService = AutoSubstitute.Resolve<IImageManagementService>();

            var imageContainer = Substitute.For<IImageContainer>();
            imageContainer.Id.Returns(Faker.Random.String());
            imageContainer.Date.Returns(Faker.Date.Recent());
            imageContainer.ContainerType.Returns(Faker.PickRandom<ImageContainerTypeEnum>());

            IList<ImageRef> returnThis = new[]
            {
                new ImageRef(Faker.Random.String(),
                    MockFileSystem.Path.Combine(Faker.System.DirectoryPathWindows(), Faker.System.FileName("png")),
                    Faker.Random.Int(1),
                    Faker.Date.Recent(),
                    imageContainer.Id,
                    imageContainer.ContainerType,
                    imageContainer.Date)
            };

            imageContainer.ImageRefs.Returns(returnThis);

            imageManagementService.ScanFolder(directoryPathWindows)
                .Returns(Observable.Return(imageContainer));

            imageManagementService.GetAllImageContainers()
                .Returns(Observable.Empty<IImageContainer>());

            var connectableImageManagementService = AutoSubstitute.Resolve<ConnectableImageManagementService>();

            var imageContainers = new ObservableCollectionExtended<IImageContainer>();

            connectableImageManagementService
                .ImageContainerCache
                .Connect()
                .Bind(imageContainers)
                .Subscribe(set =>
                {
                    ;
                });

            var imageRefs = new ObservableCollectionExtended<ImageRef>();

            connectableImageManagementService
                .ImageRefCache
                .Connect()
                .Bind(imageRefs)
                .Subscribe(set =>
                {
                    AutoResetEvent.Set();
                });

            connectableImageManagementService
                .Start()
                .Subscribe(unit =>
                {
                    AutoResetEvent.Set();
                });

            WaitOne();
     
            imageContainers.Count.Should().Be(0);
            imageRefs.Count.Should().Be(0);
            
            connectableImageManagementService
                .ScanFolder(directoryPathWindows)
                .Subscribe(unit =>
                {
                    AutoResetEvent.Set();
                });
    
            WaitOne();
     
            imageContainers.Count.Should().Be(1);

            TestSchedulerProvider.TaskPool.AdvanceBy(1);
            WaitOne();

            imageRefs.Count.Should().Be(1);

            folderRulesManagementService
                .ReceivedWithAnyArgs(1)
                .AddFolderManagementRule(default);

            var folderRule = folderRulesManagementService
                .ReceivedCalls()
                .Where(call =>call.GetMethodInfo().Name.Equals(nameof(IFolderRulesManagementService.AddFolderManagementRule)))
                .Select(call => call.GetArguments().First())
                .Cast<FolderRule>()
                .First();

            folderRule.Should().BeEquivalentTo(new FolderRule
            {
                Path = directoryPathWindows,
                Action = FolderRuleActionEnum.Once
            });
        }

        [Fact]
        public void ShouldWatch()
        {
            var folderWatcherSubject = new Subject<FileSystemEventArgs>();

            var folderWatcherService = AutoSubstitute.Resolve<IFolderWatcherService>();
            folderWatcherService.WatchFolders(default)
                .ReturnsForAnyArgs(folderWatcherSubject.AsObservable());

            var folderRulesManagementService = AutoSubstitute.Resolve<IFolderRulesManagementService>();

            var currentFolderRules = Fakers.FolderRuleFaker
                .GenerateLazy(3)
                .ToList();

            folderRulesManagementService.GetFolderManagementRules()
                .ReturnsForAnyArgs(Observable.Return(currentFolderRules));

            var imageManagementService = AutoSubstitute.Resolve<IImageManagementService>();

            var imageContainer = Substitute.For<IImageContainer>();
            imageContainer.Id.Returns(Faker.Random.String());
            imageContainer.Date.Returns(Faker.Date.Recent());
            imageContainer.ContainerType.Returns(Faker.PickRandom<ImageContainerTypeEnum>());

            var returnThis = new[]
            {
                new ImageRef(Faker.Random.String(),
                    MockFileSystem.Path.Combine(Faker.System.DirectoryPathWindows(), Faker.System.FileName("png")),
                    Faker.Random.Int(1),
                    Faker.Date.Recent(),
                    imageContainer.Id,
                    imageContainer.ContainerType,
                    imageContainer.Date)
            };

            imageContainer.ImageRefs.Returns(returnThis);

            imageManagementService.GetAllImageContainers()
                .Returns(Observable.Return(imageContainer));

            var connectableImageManagementService = AutoSubstitute.Resolve<ConnectableImageManagementService>();

            var imageContainers = new ObservableCollectionExtended<IImageContainer>();

            connectableImageManagementService
                .ImageContainerCache
                .Connect()
                .Bind(imageContainers)
                .Subscribe(set => { });

            var imageRefs = new ObservableCollectionExtended<ImageRef>();

            connectableImageManagementService
                .ImageRefCache
                .Connect()
                .Bind(imageRefs)
                .Subscribe(set =>
                {
                    AutoResetEvent.Set();
                });

            connectableImageManagementService
                .Start()
                .Subscribe(unit =>
                {
                    AutoResetEvent.Set();
                });

            WaitOne();

            imageContainers.Count.Should().Be(1);

            TestSchedulerProvider.TaskPool.AdvanceBy(1);
            WaitOne();

            imageRefs.Count.Should().Be(1);

            folderWatcherService.Received(1)
                .WatchFolders(currentFolderRules);

            var fileSystemEventArgs = new FileSystemEventArgs(
                WatcherChangeTypes.Created, 
                Faker.System.DirectoryPathWindows(), 
                Faker.System.FileName("jpg"));

            folderWatcherSubject.OnNext(fileSystemEventArgs);
            
            fileSystemEventArgs = new FileSystemEventArgs(
                WatcherChangeTypes.Deleted, 
                Faker.System.DirectoryPathWindows(), 
                Faker.System.FileName("jpg"));

            folderWatcherSubject.OnNext(fileSystemEventArgs);
        }
    }
}