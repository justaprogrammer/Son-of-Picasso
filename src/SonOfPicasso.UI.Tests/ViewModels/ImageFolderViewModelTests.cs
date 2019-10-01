using SonOfPicasso.Testing.Common;
using SonOfPicasso.UI.ViewModels;
using Xunit;
using Xunit.Abstractions;

namespace SonOfPicasso.UI.Tests.ViewModels
{
    public class ImageFolderViewModelTests : UnitTestsBase
    {
        public ImageFolderViewModelTests(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        [Fact]
        public void CanActivate()
        {
            var imageFolderViewModel = AutoSubstitute.Resolve<ImageFolderViewModel>();
            imageFolderViewModel.Activator.Activate();
        }
    }
}