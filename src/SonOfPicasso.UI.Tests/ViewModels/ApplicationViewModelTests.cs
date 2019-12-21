using System;
using System.Linq;
using DynamicData;
using FluentAssertions;
using MoreLinq;
using NSubstitute;
using SonOfPicasso.Core.Interfaces;
using SonOfPicasso.Core.Model;
using SonOfPicasso.Testing.Common;
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
            _imageContainers = Fakers.FolderFaker
                .GenerateForever("default,withImages")
                .DistinctBy(folder => folder.Date)
                .Take(2)
                .Select(folder => new FolderImageContainer(folder, MockFileSystem))
                .ToArray();
        }

        private readonly FolderImageContainer[] _imageContainers;

        private ImageContainerViewModel[] CreateImageContainerViewModels(ApplicationViewModel applicationViewModel)
        {
            return _imageContainers
                .Select(container => new ImageContainerViewModel(container, applicationViewModel))
                .ToArray();
        }

        [Fact]
        public void ShouldInitializeAndActivate()
        {
            using var imageContainerCache =
                new SourceCache<IImageContainer, string>(imageContainer => imageContainer.Key);

            var imageContainerManagementService = AutoSubstitute.Resolve<IImageContainerManagementService>();
            imageContainerManagementService.ImageContainerCache.Returns(imageContainerCache);

            var applicationViewModel = AutoSubstitute.Resolve<ApplicationViewModel>();
            applicationViewModel.Activator.Activate();

            applicationViewModel.ImageContainers.Should().HaveCount(0);
            applicationViewModel.AlbumImageContainers.Should().HaveCount(0);

            imageContainerCache.AddOrUpdate(_imageContainers);

            TestSchedulerProvider.MainThreadScheduler.AdvanceBy(1);

            applicationViewModel.ImageContainers.Should().HaveCount(2);
            applicationViewModel.AlbumImageContainers.Should().HaveCount(0);
        }

        [Fact]
        public void ShouldSelectImages()
        {
            using var imageContainerCache =
                new SourceCache<IImageContainer, string>(imageContainer => imageContainer.Key);

            var imageContainerManagementService = AutoSubstitute.Resolve<IImageContainerManagementService>();
            imageContainerManagementService.ImageContainerCache.Returns(imageContainerCache);

            var applicationViewModel = AutoSubstitute.Resolve<ApplicationViewModel>();
            applicationViewModel.Activator.Activate();

            applicationViewModel.TrayImages.Should().HaveCount(0);

            var imageContainerViewModels = CreateImageContainerViewModels(applicationViewModel);

            imageContainerCache.AddOrUpdate(_imageContainers);

            var imageContainerViewModel = Faker.PickRandom(imageContainerViewModels);
            var imageViewModel = Faker.PickRandom(imageContainerViewModel.ImageViewModels);

            applicationViewModel.ChangeSelectedImages(new[] {imageViewModel}, Array.Empty<ImageViewModel>());

            TestSchedulerProvider.MainThreadScheduler.AdvanceBy(1);

            applicationViewModel.TrayImages.Should().HaveCount(1);
            applicationViewModel.TrayImages.First().ImageViewModel.Should().Be(imageViewModel);
        }

        [Fact]
        public void ShouldSelectImageContainer()
        {
            using var imageContainerCache =
                new SourceCache<IImageContainer, string>(imageContainer => imageContainer.Key);

            var imageContainerManagementService = AutoSubstitute.Resolve<IImageContainerManagementService>();
            imageContainerManagementService.ImageContainerCache.Returns(imageContainerCache);

            var applicationViewModel = AutoSubstitute.Resolve<ApplicationViewModel>();
            applicationViewModel.Activator.Activate();

            applicationViewModel.SelectedImageContainer.Should().BeNull();
            applicationViewModel.TrayImages.Should().HaveCount(0);

            var imageContainerViewModels = CreateImageContainerViewModels(applicationViewModel);

            imageContainerCache.AddOrUpdate(_imageContainers);

            var imageContainerViewModel = Faker.PickRandom(imageContainerViewModels);

            applicationViewModel.SelectedImageContainer = imageContainerViewModel;
            applicationViewModel.SelectedImageContainer.Should().Be(imageContainerViewModel);

            TestSchedulerProvider.MainThreadScheduler.AdvanceBy(1);
            applicationViewModel.TrayImages.Should().BeEmpty();
        }

        [Fact]
        public void ShouldSelectImageContainerAndAddImage()
        {
            using var imageContainerCache =
                new SourceCache<IImageContainer, string>(imageContainer => imageContainer.Key);

            var imageContainerManagementService = AutoSubstitute.Resolve<IImageContainerManagementService>();
            imageContainerManagementService.ImageContainerCache.Returns(imageContainerCache);

            var applicationViewModel = AutoSubstitute.Resolve<ApplicationViewModel>();
            applicationViewModel.Activator.Activate();

            applicationViewModel.SelectedImageContainer.Should().BeNull();
            applicationViewModel.TrayImages.Should().HaveCount(0);

            var imageContainerViewModels = CreateImageContainerViewModels(applicationViewModel);

            imageContainerCache.AddOrUpdate(_imageContainers);

            applicationViewModel.SelectedImageContainer = imageContainerViewModels[0];
            applicationViewModel.SelectedImageContainer.Should().Be(imageContainerViewModels[0]);

            TestSchedulerProvider.MainThreadScheduler.AdvanceBy(1);
            applicationViewModel.TrayImages.Should().BeEmpty();

            var imageViewModel = Faker.PickRandom(imageContainerViewModels[1].ImageViewModels);
            applicationViewModel.ChangeSelectedImages(new []{imageViewModel}, Array.Empty<ImageViewModel>());

            TestSchedulerProvider.MainThreadScheduler.AdvanceBy(1);

            applicationViewModel.SelectedImageContainer.Should().BeNull();
            applicationViewModel.TrayImages.Should().HaveCount(imageContainerViewModels[0].ImageRefs.Count + 1);
        }

        [Fact]
        public void ShouldClearSelectedImagesOnSelectImageContainer()
        {
            using var imageContainerCache =
                new SourceCache<IImageContainer, string>(imageContainer => imageContainer.Key);

            var imageContainerManagementService = AutoSubstitute.Resolve<IImageContainerManagementService>();
            imageContainerManagementService.ImageContainerCache.Returns(imageContainerCache);

            var applicationViewModel = AutoSubstitute.Resolve<ApplicationViewModel>();
            applicationViewModel.Activator.Activate();

            applicationViewModel.SelectedImageContainer.Should().BeNull();
            applicationViewModel.TrayImages.Should().HaveCount(0);

            var imageContainerViewModels = CreateImageContainerViewModels(applicationViewModel);

            imageContainerCache.AddOrUpdate(_imageContainers);

            var imageContainerViewModel = Faker.PickRandom(imageContainerViewModels);
            var imageViewModel = Faker.PickRandom(imageContainerViewModel.ImageViewModels);

            applicationViewModel.ChangeSelectedImages(new[] {imageViewModel}, Array.Empty<ImageViewModel>());

            TestSchedulerProvider.MainThreadScheduler.AdvanceBy(1);

            applicationViewModel.TrayImages.Should().HaveCount(1);
            applicationViewModel.TrayImages[0].ImageViewModel.Should().Be(imageViewModel);

            applicationViewModel.SelectedImageContainer = imageContainerViewModel;

            applicationViewModel.SelectedImageContainer.Should().Be(imageContainerViewModel);

            TestSchedulerProvider.MainThreadScheduler.AdvanceBy(1);
            applicationViewModel.TrayImages.Should().BeEmpty();
        }

        [Fact]
        public void ShouldNotClearSelectedImagesOnSelectImageContainerWhenAnyPinned()
        {
            using var imageContainerCache =
                new SourceCache<IImageContainer, string>(imageContainer => imageContainer.Key);

            var imageContainerManagementService = AutoSubstitute.Resolve<IImageContainerManagementService>();
            imageContainerManagementService.ImageContainerCache.Returns(imageContainerCache);

            var applicationViewModel = AutoSubstitute.Resolve<ApplicationViewModel>();
            applicationViewModel.Activator.Activate();

            applicationViewModel.SelectedImageContainer.Should().BeNull();
            applicationViewModel.TrayImages.Should().HaveCount(0);

            var imageContainerViewModels = CreateImageContainerViewModels(applicationViewModel);

            imageContainerCache.AddOrUpdate(_imageContainers);

            var imageContainerViewModel = Faker.PickRandom(imageContainerViewModels);
            var imageViewModel = Faker.PickRandom(imageContainerViewModel.ImageViewModels);

            applicationViewModel.ChangeSelectedImages(new[] {imageViewModel}, Array.Empty<ImageViewModel>());

            TestSchedulerProvider.MainThreadScheduler.AdvanceBy(1);

            applicationViewModel.TrayImages.Should().HaveCount(1);
            applicationViewModel.TrayImages[0].ImageViewModel.Should().Be(imageViewModel);
            applicationViewModel.TrayImages[0].Pinned.Should().BeFalse();

            applicationViewModel.TrayImages[0].Pinned = true;

            applicationViewModel.TrayImages[0].Pinned.Should().BeTrue();

            applicationViewModel.SelectedImageContainer = imageContainerViewModel;
            
            applicationViewModel.SelectedImageContainer.Should().BeNull();

            TestSchedulerProvider.MainThreadScheduler.AdvanceBy(1);

            applicationViewModel.TrayImages.Should().HaveCount(1);
            applicationViewModel.TrayImages[0].ImageViewModel.Should().Be(imageViewModel);
        }
    }
}