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
        public void ShouldInitializeAndActivate()
        {
            var imageContainerViewModel = AutoSubstitute.Resolve<ImageContainerViewModel>();
            var applicationViewModel = AutoSubstitute.Resolve<ApplicationViewModel>();

            var folder = Fakers.FolderFaker.Generate("default,withImages");
            var folderImageContainer = new FolderImageContainer(folder);

            imageContainerViewModel.Initialize(folderImageContainer, applicationViewModel);
            ActivateContainerViewModel(2, imageContainerViewModel);

            TestSchedulerProvider.MainThreadScheduler.AdvanceBy(1);
        }
    }
}