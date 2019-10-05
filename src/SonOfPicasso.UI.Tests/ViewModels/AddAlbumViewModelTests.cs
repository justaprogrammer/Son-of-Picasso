using SonOfPicasso.Testing.Common;
using SonOfPicasso.UI.ViewModels;
using Xunit;
using Xunit.Abstractions;

namespace SonOfPicasso.UI.Tests.ViewModels
{
    public class AddAlbumViewModelTests : ViewModelTestsBase
    {
        public AddAlbumViewModelTests(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        [Fact]
        public void CanActivate()
        {
            var addAlbumViewModel = AutoSubstitute.Resolve<AddAlbumViewModel>();
            addAlbumViewModel.Activator.Activate();
        }
    }
}
