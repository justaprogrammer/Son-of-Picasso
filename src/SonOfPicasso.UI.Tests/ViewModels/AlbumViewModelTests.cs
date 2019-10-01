using SonOfPicasso.Testing.Common;
using SonOfPicasso.UI.ViewModels;
using Xunit;
using Xunit.Abstractions;

namespace SonOfPicasso.UI.Tests.ViewModels
{
    public class AlbumViewModelTests : UnitTestsBase
    {
        public AlbumViewModelTests(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        [Fact]
        public void CanActivate()
        {
            var albumViewModel = AutoSubstitute.Resolve<AlbumViewModel>();
            albumViewModel.Activator.Activate();
        }
    }
}
