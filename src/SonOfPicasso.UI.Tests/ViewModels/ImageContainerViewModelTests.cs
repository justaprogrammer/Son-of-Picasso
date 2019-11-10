using System.Collections.Generic;
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

        [Fact(Skip = "Broken")]
        public void ShouldInitializeAndActivate()
        {
            var imageContainerViewModel = AutoSubstitute.Resolve<ImageContainerViewModel>();
            var applicationViewModel = AutoSubstitute.Resolve<ApplicationViewModel>();

            var folder = Fakers.FolderFaker.Generate("default,withImages");
            var folderImageContainer = new FolderImageContainer(folder, MockFileSystem);

            imageContainerViewModel.Initialize(folderImageContainer, applicationViewModel);
            imageContainerViewModel.Activator.Activate();
            TestSchedulerProvider.MainThreadScheduler.AdvanceBy(1);
        }
    }
}