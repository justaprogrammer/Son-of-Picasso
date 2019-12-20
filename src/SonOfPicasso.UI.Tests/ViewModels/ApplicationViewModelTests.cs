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
        }

        [Fact]
        public void ShouldInitializeAndActivate()
        {
            using var imageContainerCache = new SourceCache<IImageContainer, string>(imageContainer => imageContainer.Key);

            var imageContainerManagementService = AutoSubstitute.Resolve<IImageContainerManagementService>();
            imageContainerManagementService.ImageContainerCache.Returns(imageContainerCache);

            var applicationViewModel = AutoSubstitute.Resolve<ApplicationViewModel>();
            applicationViewModel.Activator.Activate();

            applicationViewModel.ImageContainers.Should().HaveCount(0);
            applicationViewModel.AlbumImageContainers.Should().HaveCount(0);

            var folders = Fakers.FolderFaker
                .GenerateForever("default,withImages")
                .DistinctBy(folder => folder.Date)
                .Take(2)
                .ToArray();

            var imageContainers = folders
                .Select(folder => new FolderImageContainer(folder, MockFileSystem))
                .ToArray();

            imageContainerCache.AddOrUpdate(imageContainers);

            TestSchedulerProvider.MainThreadScheduler.AdvanceBy(1);

            applicationViewModel.ImageContainers.Should().HaveCount(2);
            applicationViewModel.AlbumImageContainers.Should().HaveCount(0);
        }

        [Fact]
        public void ShouldSelectImages()
        {
            using var imageContainerCache = new SourceCache<IImageContainer, string>(imageContainer => imageContainer.Key);

            var imageContainerManagementService = AutoSubstitute.Resolve<IImageContainerManagementService>();
            imageContainerManagementService.ImageContainerCache.Returns(imageContainerCache);

            var applicationViewModel = AutoSubstitute.Resolve<ApplicationViewModel>();
            applicationViewModel.Activator.Activate();
            
            applicationViewModel.TrayImages.Should().HaveCount(0);

            var folders = Fakers.FolderFaker
                .GenerateForever("default,withImages")
                .DistinctBy(folder => folder.Date)
                .Take(2)
                .ToArray();

            var imageContainers = folders
                .Select(folder => new FolderImageContainer(folder, MockFileSystem))
                .ToArray();

            var imageContainerViewModels = imageContainers
                .Select(container => new ImageContainerViewModel(container, applicationViewModel))
                .ToArray();

            imageContainerCache.AddOrUpdate(imageContainers);

            var imageContainerViewModel = Faker.PickRandom(imageContainerViewModels);
            var imageViewModel = Faker.PickRandom(imageContainerViewModel.ImageViewModels);

            applicationViewModel.ChangeSelectedImages(new []{imageViewModel}, Array.Empty<ImageViewModel>());

            TestSchedulerProvider.MainThreadScheduler.AdvanceBy(1);

            applicationViewModel.TrayImages.Should().HaveCount(1);
        }
    }
}