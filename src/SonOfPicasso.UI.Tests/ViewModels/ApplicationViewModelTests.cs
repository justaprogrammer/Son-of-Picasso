using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using FluentAssertions;
using MoreLinq;
using NSubstitute;
using SonOfPicasso.Core.Interfaces;
using SonOfPicasso.Core.Model;
using SonOfPicasso.Testing.Common;
using SonOfPicasso.UI.Interfaces;
using SonOfPicasso.UI.ViewModels;
using Xunit;
using Xunit.Abstractions;

namespace SonOfPicasso.UI.Tests.ViewModels
{
    public class ApplicationViewModelTests : ViewModelTestsBase
    {
        public ApplicationViewModelTests(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        [Fact(Skip = "Broken")]
        public void ShouldInitializeAndActivate()
        {
            var imageManagementService = AutoSubstitute.Resolve<IImageContainerOperationService>();

            var folders = Fakers.FolderFaker
                .GenerateForever("default,withImages")
                .DistinctBy(folder => folder.Date)
                .Take(2)
                .ToArray();

            var imageContainers = folders
                .Select(folder => new FolderImageContainer(folder, MockFileSystem))
                .ToArray();

            imageManagementService.GetAllImageContainers()
                .Returns(imageContainers
                    .ToObservable()
                    .SubscribeOn(TestSchedulerProvider.TaskPool));

            var applicationViewModel = AutoSubstitute.Resolve<ApplicationViewModel>();
            applicationViewModel.Activator.Activate();

            imageManagementService.Received(1)
                .GetAllImageContainers();

            TestSchedulerProvider.TaskPool.AdvanceBy(1);
            TestSchedulerProvider.MainThreadScheduler.AdvanceBy(2);

            applicationViewModel.ImageContainers.Should().HaveCount(2);
            foreach (var imageContainerViewModel in applicationViewModel.ImageContainers)
                imageContainerViewModel.Activator.Activate();

            applicationViewModel.AlbumImageContainers.Should().HaveCount(0);
            applicationViewModel.Images.Should().HaveCount(8);
        }

        [Fact(Skip = "Broken")]
        public void ShouldSelectAndPinImages()
        {
            var imageManagementService = AutoSubstitute.Resolve<IImageContainerOperationService>();

            var folders = Fakers.FolderFaker
                .GenerateForever("default,withImages")
                .DistinctBy(folder => folder.Date)
                .Take(2)
                .ToArray();

            var imageContainers = folders
                .Select(folder => new FolderImageContainer(folder, MockFileSystem))
                .ToArray();

            imageManagementService.GetAllImageContainers()
                .Returns(imageContainers
                    .ToObservable()
                    .SubscribeOn(TestSchedulerProvider.TaskPool));

            var applicationViewModel = AutoSubstitute.Resolve<ApplicationViewModel>();
            applicationViewModel.Activator.Activate();

            imageManagementService.Received(1)
                .GetAllImageContainers();

            TestSchedulerProvider.TaskPool.AdvanceBy(1);
            TestSchedulerProvider.MainThreadScheduler.AdvanceBy(2);

            foreach (var imageContainerViewModel in applicationViewModel.ImageContainers)
                imageContainerViewModel.Activator.Activate();

            var randomImages = Faker.PickRandom(applicationViewModel.Images, 2)
                .ToArray();

            applicationViewModel.ChangeSelectedImages(randomImages, Enumerable.Empty<ImageViewModel>());
           
            TestSchedulerProvider.MainThreadScheduler.AdvanceBy(1);
            applicationViewModel.TrayImages.Should().HaveCount(2);

            applicationViewModel.TrayImages.Select(model => model.Pinned).Should().AllBeEquivalentTo(false);

            applicationViewModel.PinSelectedItems.Execute(applicationViewModel.TrayImages)
                .Subscribe(unit => { }, () => AutoResetEvent.Set());

            TestSchedulerProvider.MainThreadScheduler.AdvanceBy(1);

            WaitOne();
            
            applicationViewModel.TrayImages.Should().HaveCount(2);
            applicationViewModel.TrayImages.Select(model => model.Pinned).Should().AllBeEquivalentTo(true);

            applicationViewModel.ChangeSelectedImages(Enumerable.Empty<ImageViewModel>(), randomImages);
    
            TestSchedulerProvider.MainThreadScheduler.AdvanceBy(1);
            applicationViewModel.TrayImages.Should().HaveCount(2);
        }

        [Fact(Skip = "Broken")]
        public void ShouldDisplayPinnedAndNonPinned()
        {
            var imageManagementService = AutoSubstitute.Resolve<IImageContainerOperationService>();

            var folders = Fakers.FolderFaker
                .GenerateForever("default,withImages")
                .DistinctBy(folder => folder.Date)
                .Take(2)
                .ToArray();

            var imageContainers = folders
                .Select(folder => new FolderImageContainer(folder, MockFileSystem))
                .ToArray();

            imageManagementService.GetAllImageContainers()
                .Returns(imageContainers
                    .ToObservable()
                    .SubscribeOn(TestSchedulerProvider.TaskPool));

            var applicationViewModel = AutoSubstitute.Resolve<ApplicationViewModel>();
            applicationViewModel.Activator.Activate();

            imageManagementService.Received(1)
                .GetAllImageContainers();

            TestSchedulerProvider.TaskPool.AdvanceBy(1);
            TestSchedulerProvider.MainThreadScheduler.AdvanceBy(2);

            foreach (var imageContainerViewModel in applicationViewModel.ImageContainers.ToArray())
                imageContainerViewModel.Activator.Activate();

            var randomImages = Faker.PickRandom(applicationViewModel.Images, 4)
                .Batch(2)
                .ToArray();

            applicationViewModel.ChangeSelectedImages(randomImages[0], Enumerable.Empty<ImageViewModel>());

            TestSchedulerProvider.MainThreadScheduler.AdvanceBy(1);
            applicationViewModel.TrayImages.Should().HaveCount(2);

            applicationViewModel.TrayImages.Select(model => model.Pinned).Should().AllBeEquivalentTo(false);

            applicationViewModel.PinSelectedItems.Execute(applicationViewModel.TrayImages)
                .Subscribe(unit => { }, () => AutoResetEvent.Set());

            TestSchedulerProvider.MainThreadScheduler.AdvanceBy(1);

            WaitOne();
            
            applicationViewModel.TrayImages.Should().HaveCount(2);
            applicationViewModel.TrayImages.Select(model => model.Pinned).Should().AllBeEquivalentTo(true);

            applicationViewModel.ChangeSelectedImages(randomImages[1], randomImages[0]);
    
            TestSchedulerProvider.MainThreadScheduler.AdvanceBy(1);
            applicationViewModel.TrayImages.Should().HaveCount(4);
        }

        [Fact(Skip = "Broken")]
        public void ShouldClearPinnedAllIfNoneSelectedAndConfirm()
        {
            var imageManagementService = AutoSubstitute.Resolve<IImageContainerOperationService>();

            var folders = Fakers.FolderFaker
                .GenerateForever("default,withImages")
                .DistinctBy(folder => folder.Date)
                .Take(2)
                .ToArray();

            var imageContainers = folders
                .Select(folder => new FolderImageContainer(folder, MockFileSystem))
                .ToArray();

            imageManagementService.GetAllImageContainers()
                .Returns(imageContainers
                    .ToObservable()
                    .SubscribeOn(TestSchedulerProvider.TaskPool));

            var applicationViewModel = AutoSubstitute.Resolve<ApplicationViewModel>();
            applicationViewModel.Activator.Activate();

            imageManagementService.Received(1)
                .GetAllImageContainers();

            TestSchedulerProvider.TaskPool.AdvanceBy(1);
            TestSchedulerProvider.MainThreadScheduler.AdvanceBy(2);

            foreach (var imageContainerViewModel in applicationViewModel.ImageContainers.ToArray())
                imageContainerViewModel.Activator.Activate();

            var randomImages = Faker.PickRandom(applicationViewModel.Images, 4)
                .ToArray();

            var randomImageBatches = randomImages
                .Batch(2)
                .ToArray();

            applicationViewModel.ChangeSelectedImages(randomImageBatches[0], Enumerable.Empty<ImageViewModel>());

            TestSchedulerProvider.MainThreadScheduler.AdvanceBy(4);

            applicationViewModel.TrayImages.Select(model => model.Pinned).Should().AllBeEquivalentTo(false);

            applicationViewModel.PinSelectedItems.Execute(applicationViewModel.TrayImages)
                .Subscribe(unit => { }, () => AutoResetEvent.Set());

            TestSchedulerProvider.MainThreadScheduler.AdvanceBy(1);

            WaitOne();
            
            applicationViewModel.ChangeSelectedImages(randomImageBatches[1], randomImageBatches[0]);
    
            TestSchedulerProvider.MainThreadScheduler.AdvanceBy(1);
            applicationViewModel.TrayImages.Should().HaveCount(4);

            applicationViewModel.ConfirmClearTrayItemsInteraction.RegisterHandler(context =>
            {
                context.SetOutput(true);
            });

            applicationViewModel.ClearTrayItems.Execute((applicationViewModel.TrayImages.ToArray(), true))
                .Subscribe(unit => { }, () => AutoResetEvent.Set());

            TestSchedulerProvider.TaskPool.AdvanceBy(4);
            TestSchedulerProvider.MainThreadScheduler.AdvanceBy(4);
       
            WaitOne();

            applicationViewModel.TrayImages.Should().HaveCount(0);
        }

        [Fact(Skip = "Broken")]
        public void ShouldClearPinnedAllIfNoneSelectedAndDecline()
        {
            var imageManagementService = AutoSubstitute.Resolve<IImageContainerOperationService>();

            var folders = Fakers.FolderFaker
                .GenerateForever("default,withImages")
                .DistinctBy(folder => folder.Date)
                .Take(2)
                .ToArray();

            var imageContainers = folders
                .Select(folder => new FolderImageContainer(folder, MockFileSystem))
                .ToArray();

            imageManagementService.GetAllImageContainers()
                .Returns(imageContainers
                    .ToObservable()
                    .SubscribeOn(TestSchedulerProvider.TaskPool));

            var applicationViewModel = AutoSubstitute.Resolve<ApplicationViewModel>();
            applicationViewModel.Activator.Activate();

            imageManagementService.Received(1)
                .GetAllImageContainers();

            TestSchedulerProvider.TaskPool.AdvanceBy(1);
            TestSchedulerProvider.MainThreadScheduler.AdvanceBy(2);

            foreach (var imageContainerViewModel in applicationViewModel.ImageContainers.ToArray())
                imageContainerViewModel.Activator.Activate();

            var randomImages = Faker.PickRandom(applicationViewModel.Images, 4)
                .ToArray();

            var randomImageBatches = randomImages
                .Batch(2)
                .ToArray();

            applicationViewModel.ChangeSelectedImages(randomImageBatches[0], Enumerable.Empty<ImageViewModel>());

            TestSchedulerProvider.MainThreadScheduler.AdvanceBy(4);

            applicationViewModel.TrayImages.Select(model => model.Pinned).Should().AllBeEquivalentTo(false);

            applicationViewModel.PinSelectedItems.Execute(applicationViewModel.TrayImages)
                .Subscribe(unit => { }, () => AutoResetEvent.Set());

            TestSchedulerProvider.MainThreadScheduler.AdvanceBy(1);

            WaitOne();
            
            applicationViewModel.ChangeSelectedImages(randomImageBatches[1], randomImageBatches[0]);
    
            TestSchedulerProvider.MainThreadScheduler.AdvanceBy(1);
            applicationViewModel.TrayImages.Should().HaveCount(4);

            applicationViewModel.ConfirmClearTrayItemsInteraction.RegisterHandler(context =>
            {
                context.SetOutput(false);
            });

            applicationViewModel.ClearTrayItems.Execute((applicationViewModel.TrayImages.ToArray(), true))
                .Subscribe(unit => { }, () => AutoResetEvent.Set());

            TestSchedulerProvider.TaskPool.AdvanceBy(4);
            TestSchedulerProvider.MainThreadScheduler.AdvanceBy(4);
       
            WaitOne();

            applicationViewModel.TrayImages.Should().HaveCount(4);
        }

        [Fact(Skip = "Broken")]
        public void ShouldExecuteFolderManagerAndCancel()
        {
            var imageManagementService = AutoSubstitute.Resolve<IImageContainerOperationService>();
            var folderRulesManagementService = AutoSubstitute.Resolve<IFolderRulesManagementService>();

            var folders = Fakers.FolderFaker
                .GenerateForever("default,withImages")
                .DistinctBy(folder => folder.Date)
                .Take(2)
                .ToArray();

            var imageContainers = folders
                .Select(folder => new FolderImageContainer(folder, MockFileSystem))
                .ToArray();

            imageManagementService.GetAllImageContainers()
                .Returns(imageContainers
                    .ToObservable()
                    .SubscribeOn(TestSchedulerProvider.TaskPool));

            var applicationViewModel = AutoSubstitute.Resolve<ApplicationViewModel>();
            applicationViewModel.Activator.Activate();

            imageManagementService.Received(1)
                .GetAllImageContainers();

            TestSchedulerProvider.TaskPool.AdvanceBy(1);
            TestSchedulerProvider.MainThreadScheduler.AdvanceBy(2);

            applicationViewModel.ImageContainers.Should().HaveCount(2);
            foreach (var imageContainerViewModel in applicationViewModel.ImageContainers)
                imageContainerViewModel.Activator.Activate();

            applicationViewModel.FolderManagerInteraction.RegisterHandler(context =>
            {
                context.SetOutput(null);
            });

            applicationViewModel.FolderManager.Execute(Unit.Default)
                .Subscribe(unit =>
                {
                    AutoResetEvent.Set();
                });

            TestSchedulerProvider.TaskPool.AdvanceBy(2);

            WaitOne();

            folderRulesManagementService
                .DidNotReceive()
                .ResetFolderManagementRules(default);
        }
    }
}