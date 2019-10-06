using System.Linq;
using System.Reactive.Linq;
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
        public void ShouldHandleImageSelection()
        {
            var imageManagementService = AutoSubstitute.Resolve<IImageManagementService>();

            var folders = Fakers.FolderFaker
                .GenerateForever("default,withImages")
                .DistinctBy(folder => folder.Date)
                .Take(2)
                .ToArray();

            var imageContainers = folders
                .Select(folder => new FolderImageContainer(folder))
                .ToArray();

            imageManagementService.GetAllImageContainers()
                .Returns(imageContainers
                    .ToObservable()
                    .SubscribeOn(TestSchedulerProvider.TaskPool));

            var applicationViewModel = AutoSubstitute.Resolve<ApplicationViewModel>();
            applicationViewModel.Activator.Activate();

            TestSchedulerProvider.TaskPool.AdvanceBy(1);
            TestSchedulerProvider.MainThreadScheduler.AdvanceBy(2);

            ActivateContainerViewModel(applicationViewModel.ImageContainerViewModels.ToArray());
            
            TestSchedulerProvider.MainThreadScheduler.AdvanceBy(2);

            applicationViewModel.SelectedImage.Should().BeNull();
            applicationViewModel.SelectedImageRow.Should().BeNull();
            applicationViewModel.SelectedImageContainer.Should().BeNull();

            applicationViewModel.ImageContainerViewModels[0].ImageRowViewModels[0].SelectedImage
                = applicationViewModel.ImageContainerViewModels[0].ImageRowViewModels[0].ImageViewModels[0];

            applicationViewModel.SelectedImageContainer
                .Should()
                .Be(applicationViewModel.ImageContainerViewModels[0]);

            applicationViewModel.SelectedImageRow
                .Should()
                .Be(applicationViewModel.ImageContainerViewModels[0].ImageRowViewModels[0]);

            applicationViewModel.SelectedImage
                .Should()
                .Be(applicationViewModel.ImageContainerViewModels[0].ImageRowViewModels[0].ImageViewModels[0]);

            applicationViewModel.ImageContainerViewModels[0].SelectedImageRow
                .Should()
                .Be(applicationViewModel.ImageContainerViewModels[0].ImageRowViewModels[0]);

            applicationViewModel.ImageContainerViewModels[0].SelectedImage
                .Should()
                .Be(applicationViewModel.ImageContainerViewModels[0].ImageRowViewModels[0].ImageViewModels[0]);

            applicationViewModel.ImageContainerViewModels[1].SelectedImageRow
                .Should().BeNull();

            applicationViewModel.ImageContainerViewModels[1].SelectedImage
                .Should().BeNull();

            applicationViewModel.ImageContainerViewModels[0].ImageRowViewModels[0].SelectedImage
                = applicationViewModel.ImageContainerViewModels[0].ImageRowViewModels[0].ImageViewModels[1];

            applicationViewModel.SelectedImageContainer
                .Should()
                .Be(applicationViewModel.ImageContainerViewModels[0]);

            applicationViewModel.SelectedImageRow
                .Should()
                .Be(applicationViewModel.ImageContainerViewModels[0].ImageRowViewModels[0]);

            applicationViewModel.SelectedImage
                .Should()
                .Be(applicationViewModel.ImageContainerViewModels[0].ImageRowViewModels[0].ImageViewModels[1]);

            applicationViewModel.ImageContainerViewModels[0].SelectedImageRow
                .Should()
                .Be(applicationViewModel.ImageContainerViewModels[0].ImageRowViewModels[0]);

            applicationViewModel.ImageContainerViewModels[0].SelectedImage
                .Should()
                .Be(applicationViewModel.ImageContainerViewModels[0].ImageRowViewModels[0].ImageViewModels[1]);

            applicationViewModel.ImageContainerViewModels[1].SelectedImageRow
                .Should().BeNull();

            applicationViewModel.ImageContainerViewModels[1].SelectedImage
                .Should().BeNull();

            applicationViewModel.ImageContainerViewModels[0].ImageRowViewModels[1].SelectedImage
                = applicationViewModel.ImageContainerViewModels[0].ImageRowViewModels[1].ImageViewModels[0];

            applicationViewModel.SelectedImageContainer
                .Should()
                .Be(applicationViewModel.ImageContainerViewModels[0]);

            applicationViewModel.SelectedImageRow
                .Should()
                .Be(applicationViewModel.ImageContainerViewModels[0].ImageRowViewModels[1]);

            applicationViewModel.SelectedImage
                .Should()
                .Be(applicationViewModel.ImageContainerViewModels[0].ImageRowViewModels[1].ImageViewModels[0]);

            applicationViewModel.ImageContainerViewModels[0].SelectedImageRow
                .Should()
                .Be(applicationViewModel.ImageContainerViewModels[0].ImageRowViewModels[1]);

            applicationViewModel.ImageContainerViewModels[0].SelectedImage
                .Should()
                .Be(applicationViewModel.ImageContainerViewModels[0].ImageRowViewModels[1].ImageViewModels[0]);

            applicationViewModel.ImageContainerViewModels[1].SelectedImageRow
                .Should().BeNull();

            applicationViewModel.ImageContainerViewModels[1].SelectedImage
                .Should().BeNull();

            applicationViewModel.ImageContainerViewModels[1].ImageRowViewModels[0].SelectedImage
                = applicationViewModel.ImageContainerViewModels[1].ImageRowViewModels[0].ImageViewModels[0];

            applicationViewModel.SelectedImageContainer
                .Should()
                .Be(applicationViewModel.ImageContainerViewModels[1]);

            applicationViewModel.SelectedImageRow
                .Should()
                .Be(applicationViewModel.ImageContainerViewModels[1].ImageRowViewModels[0]);

            applicationViewModel.SelectedImage
                .Should()
                .Be(applicationViewModel.ImageContainerViewModels[1].ImageRowViewModels[0].ImageViewModels[0]);

            applicationViewModel.ImageContainerViewModels[0].SelectedImageRow
                .Should().BeNull();

            applicationViewModel.ImageContainerViewModels[0].SelectedImage
                .Should().BeNull();

            applicationViewModel.ImageContainerViewModels[1].SelectedImageRow
                .Should()
                .Be(applicationViewModel.ImageContainerViewModels[1].ImageRowViewModels[0]);

            applicationViewModel.ImageContainerViewModels[1].SelectedImage
                .Should()
                .Be(applicationViewModel.ImageContainerViewModels[1].ImageRowViewModels[0].ImageViewModels[0]);
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
                .Select(folder => new FolderImageContainer(folder))
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

            applicationViewModel.ImageContainerViewModels.Count.Should().Be(2);
            ActivateContainerViewModel(applicationViewModel.ImageContainerViewModels.ToArray());
        }
    }
}