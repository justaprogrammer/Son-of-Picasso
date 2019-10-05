using System;
using System.Linq;
using FluentAssertions;
using FluentAssertions.Execution;
using ReactiveUI;
using SonOfPicasso.Core.Interfaces;
using SonOfPicasso.Core.Model;
using SonOfPicasso.Testing.Common;
using SonOfPicasso.UI.Interfaces;
using SonOfPicasso.UI.ViewModels;
using Xunit;
using Xunit.Abstractions;

namespace SonOfPicasso.UI.Tests.ViewModels
{
    public class ImageContainerViewModelTests : UnitTestsBase
    {
        public ImageContainerViewModelTests(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
            Func<ImageViewModel> imageViewModelFactory = () =>
                new ImageViewModel(AutoSubstitute.Resolve<IImageLoadingService>(), TestSchedulerProvider,
                    new ViewModelActivator());

            Func<ImageRowViewModel> imageRowViewModelFactory =
                () => new ImageRowViewModel(imageViewModelFactory, new ViewModelActivator());

            AutoSubstitute.Provide(imageRowViewModelFactory);
        }

        [Fact]
        public void ShouldHandleImageSelection()
        {
            var imageContainerViewModel = AutoSubstitute.Resolve<ImageContainerViewModel>();
            var applicationViewModel = AutoSubstitute.Resolve<IApplicationViewModel>();

            var folder = Fakers.FolderFaker.Generate("default,withImages");
            var folderImageContainer = new FolderImageContainer(folder);

            var imageRefs = folder.Images
                .Select(image => new ImageRef(image, folderImageContainer))
                .ToArray();

            imageContainerViewModel.Initialize(folderImageContainer, applicationViewModel);
            imageContainerViewModel.Activator.Activate();
            foreach (var imageRowViewModel in imageContainerViewModel.ImageRowViewModels)
            {
                imageRowViewModel.Activator.Activate();
                
                foreach (var imageViewModel in imageRowViewModel.ImageViewModels) 
                    imageViewModel.Activator.Activate();
            }

            using (new AssertionScope())
            {
                imageContainerViewModel.SelectedImageRow.Should().BeNull();
                imageContainerViewModel.SelectedImage.Should().BeNull();

                imageContainerViewModel.ImageRowViewModels[0].SelectedImage.Should().BeNull();
                imageContainerViewModel.ImageRowViewModels[1].SelectedImage.Should().BeNull();
            }

            imageContainerViewModel.ImageRowViewModels[0].SelectedImage =
                imageContainerViewModel.ImageRowViewModels[0].ImageViewModels[0];

            using (new AssertionScope())
            {
                imageContainerViewModel.SelectedImage.Should()
                    .Be(imageContainerViewModel.ImageRowViewModels[0].ImageViewModels[0]);

                imageContainerViewModel.SelectedImageRow.Should()
                    .Be(imageContainerViewModel.ImageRowViewModels[0]);

                imageContainerViewModel.ImageRowViewModels[0].SelectedImage.Should()
                    .Be(imageContainerViewModel.ImageRowViewModels[0].ImageViewModels[0]);

                imageContainerViewModel.ImageRowViewModels[1].SelectedImage.Should()
                    .BeNull();
            }

            imageContainerViewModel.ImageRowViewModels[0].SelectedImage =
                imageContainerViewModel.ImageRowViewModels[0].ImageViewModels[1];

            using (new AssertionScope())
            {
                imageContainerViewModel.SelectedImage.Should()
                    .Be(imageContainerViewModel.ImageRowViewModels[0].ImageViewModels[1]);

                imageContainerViewModel.SelectedImageRow.Should()
                    .Be(imageContainerViewModel.ImageRowViewModels[0]);

                imageContainerViewModel.ImageRowViewModels[0].SelectedImage.Should()
                    .Be(imageContainerViewModel.ImageRowViewModels[0].ImageViewModels[1]);

                imageContainerViewModel.ImageRowViewModels[1].SelectedImage.Should()
                    .BeNull();
            }

            imageContainerViewModel.ImageRowViewModels[1].SelectedImage =
                imageContainerViewModel.ImageRowViewModels[1].ImageViewModels[0];

            using (new AssertionScope())
            {
                imageContainerViewModel.SelectedImage.Should()
                    .Be(imageContainerViewModel.ImageRowViewModels[1].ImageViewModels[0]);

                imageContainerViewModel.SelectedImageRow.Should()
                    .Be(imageContainerViewModel.ImageRowViewModels[1]);

                imageContainerViewModel.ImageRowViewModels[0].SelectedImage.Should()
                    .BeNull();

                imageContainerViewModel.ImageRowViewModels[1].SelectedImage.Should()
                    .Be(imageContainerViewModel.ImageRowViewModels[1].ImageViewModels[0]);
            }

            imageContainerViewModel.ImageRowViewModels[1].SelectedImage = null;

            using (new AssertionScope())
            {
                imageContainerViewModel.SelectedImage.Should()
                    .BeNull();

                imageContainerViewModel.SelectedImageRow.Should()
                    .BeNull();

                imageContainerViewModel.ImageRowViewModels[0].SelectedImage.Should()
                    .BeNull();

                imageContainerViewModel.ImageRowViewModels[1].SelectedImage.Should()
                    .BeNull();
            }
        }

        [Fact]
        public void ShouldInitializeAndActivate()
        {
            var imageContainerViewModel = AutoSubstitute.Resolve<ImageContainerViewModel>();
            var applicationViewModel = AutoSubstitute.Resolve<IApplicationViewModel>();

            var folder = Fakers.FolderFaker.Generate("default,withImages");
            var folderImageContainer = new FolderImageContainer(folder);

            var imageRefs = folder.Images
                .Select(image => new ImageRef(image, folderImageContainer))
                .ToArray();

            imageContainerViewModel.Initialize(folderImageContainer, applicationViewModel);
            imageContainerViewModel.Activator.Activate();

            imageContainerViewModel.ImageRowViewModels.Count.Should().Be(2);
            imageContainerViewModel.ImageRowViewModels[0].ImageViewModels.Count.Should().Be(3);
            imageContainerViewModel.ImageRowViewModels[1].ImageViewModels.Count.Should().Be(1);
        }
    }
}