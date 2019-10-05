using System;
using System.Linq;
using FluentAssertions;
using FluentAssertions.Execution;
using MoreLinq;
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
    public class ImageRowViewModelTests : UnitTestsBase
    {
        public ImageRowViewModelTests(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
            Func<ImageViewModel> imageViewModelFactory = () =>
                new ImageViewModel(AutoSubstitute.Resolve<IImageLoadingService>(), TestSchedulerProvider,
                    new ViewModelActivator());

            AutoSubstitute.Provide(imageViewModelFactory);
        }

        [Fact]
        public void ShouldInitializeAndActivate()
        {
            var imageRowViewModel = AutoSubstitute.Resolve<ImageRowViewModel>();
            var imageContainerViewModel = AutoSubstitute.Resolve<IImageContainerViewModel>();

            var folder = Fakers.FolderFaker.Generate("default,withImages");
                    var folderImageContainer = new FolderImageContainer(folder);
            
            var imageRefs = folder.Images
                .Select(image => new ImageRef(image, folderImageContainer))
                .ToArray();

            imageRowViewModel.Initialize(imageRefs, imageContainerViewModel);
            imageRowViewModel.Activator.Activate();

            using (new AssertionScope())
            {
                imageRowViewModel.ImageViewModels.Count.Should().Be(imageRefs.Length);
                foreach (var imageRef in imageRefs)
                {
                    imageRowViewModel.ImageIdSet.Contains(imageRef.Id).Should().BeTrue();
                }

                foreach (var imageViewModel in imageRowViewModel.ImageViewModels)
                {
                    imageViewModel.ImageRowViewModel.Should().Be(imageRowViewModel);
                }
            }
        }
    }
}