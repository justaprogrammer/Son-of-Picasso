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

        [Fact]
        public void ShouldInitializeAndActivate()
        {
            var imageManagementService = AutoSubstitute.Resolve<IImageManagementService>();

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

            applicationViewModel.ImageContainers.Count.Should().Be(2);
            foreach (var imageContainerViewModel in applicationViewModel.ImageContainers)
                imageContainerViewModel.Activator.Activate();

            applicationViewModel.AlbumImageContainers.Count.Should().Be(0);
            applicationViewModel.Images.Count.Should().Be(8);
        }

        [Fact]
        public void ShouldSelectAndPinImages()
        {
            var imageManagementService = AutoSubstitute.Resolve<IImageManagementService>();

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
            applicationViewModel.TrayImages.Count.Should().Be(2);

            applicationViewModel.TrayImages.Select(model => model.Pinned).Should().AllBeEquivalentTo(false);

            applicationViewModel.PinSelectedItems.Execute(applicationViewModel.TrayImages)
                .Subscribe(unit => { }, () => AutoResetEvent.Set());

            TestSchedulerProvider.MainThreadScheduler.AdvanceBy(1);

            WaitOne();
            
            applicationViewModel.TrayImages.Count.Should().Be(2);
            applicationViewModel.TrayImages.Select(model => model.Pinned).Should().AllBeEquivalentTo(true);

            applicationViewModel.ChangeSelectedImages(Enumerable.Empty<ImageViewModel>(), randomImages);
    
            TestSchedulerProvider.MainThreadScheduler.AdvanceBy(1);
            applicationViewModel.TrayImages.Count.Should().Be(2);
        }

        [Fact]
        public void ShouldDisplayPinnedAndNonPinned()
        {
            var imageManagementService = AutoSubstitute.Resolve<IImageManagementService>();

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
            applicationViewModel.TrayImages.Count.Should().Be(2);

            applicationViewModel.TrayImages.Select(model => model.Pinned).Should().AllBeEquivalentTo(false);

            applicationViewModel.PinSelectedItems.Execute(applicationViewModel.TrayImages)
                .Subscribe(unit => { }, () => AutoResetEvent.Set());

            TestSchedulerProvider.MainThreadScheduler.AdvanceBy(1);

            WaitOne();
            
            applicationViewModel.TrayImages.Count.Should().Be(2);
            applicationViewModel.TrayImages.Select(model => model.Pinned).Should().AllBeEquivalentTo(true);

            applicationViewModel.ChangeSelectedImages(randomImages[1], randomImages[0]);
    
            TestSchedulerProvider.MainThreadScheduler.AdvanceBy(1);
            applicationViewModel.TrayImages.Count.Should().Be(4);
        }

        [Fact]
        public void ShouldClearPinnedAllIfNoneSelectedAndConfirm()
        {
            var imageManagementService = AutoSubstitute.Resolve<IImageManagementService>();

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
            applicationViewModel.TrayImages.Count.Should().Be(4);

            applicationViewModel.ConfirmClearTrayItemsInteraction.RegisterHandler(context =>
            {
                context.SetOutput(true);
            });

            applicationViewModel.ClearTrayItems.Execute((applicationViewModel.TrayImages.ToArray(), true))
                .Subscribe(unit => { }, () => AutoResetEvent.Set());

            TestSchedulerProvider.TaskPool.AdvanceBy(4);
            TestSchedulerProvider.MainThreadScheduler.AdvanceBy(4);
       
            WaitOne();

            applicationViewModel.TrayImages.Count.Should().Be(0);
        }

        [Fact]
        public void ShouldClearPinnedAllIfNoneSelectedAndDecline()
        {
            var imageManagementService = AutoSubstitute.Resolve<IImageManagementService>();

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
            applicationViewModel.TrayImages.Count.Should().Be(4);

            applicationViewModel.ConfirmClearTrayItemsInteraction.RegisterHandler(context =>
            {
                context.SetOutput(false);
            });

            applicationViewModel.ClearTrayItems.Execute((applicationViewModel.TrayImages.ToArray(), true))
                .Subscribe(unit => { }, () => AutoResetEvent.Set());

            TestSchedulerProvider.TaskPool.AdvanceBy(4);
            TestSchedulerProvider.MainThreadScheduler.AdvanceBy(4);
       
            WaitOne();

            applicationViewModel.TrayImages.Count.Should().Be(4);
        }

        [Fact]
        public void ShouldExecuteFolderManagerAndCancel()
        {
            var imageManagementService = AutoSubstitute.Resolve<IImageManagementService>();
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

            applicationViewModel.ImageContainers.Count.Should().Be(2);
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

        [Fact]
        public void ShouldExecuteFolderManagerAndContinue()
        {
            var imageManagementService = AutoSubstitute.Resolve<IImageManagementService>();
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

            applicationViewModel.ImageContainers.Count.Should().Be(2);
            foreach (var imageContainerViewModel in applicationViewModel.ImageContainers)
                imageContainerViewModel.Activator.Activate();

            var folderRuleViewModels = AutoSubstitute.Resolve<IObservableCollection<FolderRuleViewModel>>();
            
            var manageFolderRulesViewModel = AutoSubstitute.Resolve<IManageFolderRulesViewModel>();
            manageFolderRulesViewModel.Folders.Returns(folderRuleViewModels);

            applicationViewModel.FolderManagerInteraction.RegisterHandler(context =>
            {
                context.SetOutput(manageFolderRulesViewModel);
            });

            applicationViewModel.FolderManager.Execute(Unit.Default)
                .Subscribe(unit =>
                {
                    AutoResetEvent.Set();
                });

            TestSchedulerProvider.TaskPool.AdvanceBy(2);

            WaitOne();

            folderRulesManagementService
                .Received(1)
                .ResetFolderManagementRules(folderRuleViewModels);
        }
    }
}