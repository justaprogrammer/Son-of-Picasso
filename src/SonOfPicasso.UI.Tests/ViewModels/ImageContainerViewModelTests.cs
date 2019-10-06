using FluentAssertions;
using SonOfPicasso.Core.Model;
using SonOfPicasso.Testing.Common;
using SonOfPicasso.UI.ViewModels;
using Xunit;
using Xunit.Abstractions;

namespace SonOfPicasso.UI.Tests.ViewModels
{
    public class ImageContainerViewModelTests : ViewModelTestsBase
    {
        public ImageContainerViewModelTests(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        [Fact]
        public void ShouldHandleImageSelection()
        {
            var imageContainerViewModel = AutoSubstitute.Resolve<ImageContainerViewModel>();
            var applicationViewModel = AutoSubstitute.Resolve<ApplicationViewModel>();

            var folder = Fakers.FolderFaker.Generate("default,withImages");
            var folderImageContainer = new FolderImageContainer(folder);

            imageContainerViewModel.Initialize(folderImageContainer, applicationViewModel);
            ActivateContainerViewModel(2, imageContainerViewModel);

            imageContainerViewModel.SelectedImageRow.Should().BeNull();
            imageContainerViewModel.SelectedImage.Should().BeNull();

            imageContainerViewModel.ImageRowViewModels[0].SelectedImage.Should().BeNull();
            imageContainerViewModel.ImageRowViewModels[1].SelectedImage.Should().BeNull();

            imageContainerViewModel.ImageRowViewModels[0].SelectedImage =
                imageContainerViewModel.ImageRowViewModels[0].ImageViewModels[0];

            imageContainerViewModel.SelectedImage.Should()
                .Be(imageContainerViewModel.ImageRowViewModels[0].ImageViewModels[0]);

            imageContainerViewModel.SelectedImageRow.Should()
                .Be(imageContainerViewModel.ImageRowViewModels[0]);

            imageContainerViewModel.ImageRowViewModels[0].SelectedImage.Should()
                .Be(imageContainerViewModel.ImageRowViewModels[0].ImageViewModels[0]);

            imageContainerViewModel.ImageRowViewModels[1].SelectedImage.Should()
                .BeNull();

            imageContainerViewModel.ImageRowViewModels[0].SelectedImage =
                imageContainerViewModel.ImageRowViewModels[0].ImageViewModels[1];

            imageContainerViewModel.SelectedImage.Should()
                .Be(imageContainerViewModel.ImageRowViewModels[0].ImageViewModels[1]);

            imageContainerViewModel.SelectedImageRow.Should()
                .Be(imageContainerViewModel.ImageRowViewModels[0]);

            imageContainerViewModel.ImageRowViewModels[0].SelectedImage.Should()
                .Be(imageContainerViewModel.ImageRowViewModels[0].ImageViewModels[1]);

            imageContainerViewModel.ImageRowViewModels[1].SelectedImage.Should()
                .BeNull();

            imageContainerViewModel.ImageRowViewModels[1].SelectedImage =
                imageContainerViewModel.ImageRowViewModels[1].ImageViewModels[0];

            imageContainerViewModel.SelectedImage.Should()
                .Be(imageContainerViewModel.ImageRowViewModels[1].ImageViewModels[0]);

            imageContainerViewModel.SelectedImageRow.Should()
                .Be(imageContainerViewModel.ImageRowViewModels[1]);

            imageContainerViewModel.ImageRowViewModels[0].SelectedImage.Should()
                .BeNull();

            imageContainerViewModel.ImageRowViewModels[1].SelectedImage.Should()
                .Be(imageContainerViewModel.ImageRowViewModels[1].ImageViewModels[0]);

            imageContainerViewModel.ImageRowViewModels[1].SelectedImage = null;

            imageContainerViewModel.SelectedImage.Should()
                .BeNull();

            imageContainerViewModel.SelectedImageRow.Should()
                .BeNull();

            imageContainerViewModel.ImageRowViewModels[0].SelectedImage.Should()
                .BeNull();

            imageContainerViewModel.ImageRowViewModels[1].SelectedImage.Should()
                .BeNull();
        }

        [Fact]
        public void ShouldInitializeAndActivate()
        {
            var imageContainerViewModel = AutoSubstitute.Resolve<ImageContainerViewModel>();
            var applicationViewModel = AutoSubstitute.Resolve<ApplicationViewModel>();

            var folder = Fakers.FolderFaker.Generate("default,withImages");
            var folderImageContainer = new FolderImageContainer(folder);

            imageContainerViewModel.Initialize(folderImageContainer, applicationViewModel);
            ActivateContainerViewModel(2, imageContainerViewModel);

            TestSchedulerProvider.MainThreadScheduler.AdvanceBy(1);

            imageContainerViewModel.ImageRowViewModels.Count.Should().Be(2);
            imageContainerViewModel.ImageRowViewModels[0].ImageViewModels.Count.Should().Be(3);
            imageContainerViewModel.ImageRowViewModels[1].ImageViewModels.Count.Should().Be(1);
        }
    }
}