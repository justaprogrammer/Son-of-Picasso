using SonOfPicasso.UI.ViewModels;
using Xunit;
using Xunit.Abstractions;

namespace SonOfPicasso.UI.Tests.ViewModels
{
    public class FolderManagementViewModelTests : ViewModelTestsBase
    {
        public FolderManagementViewModelTests(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        [Fact]
        public void ShouldInitializeAndActivate()
        {
            MockFileSystem.AddDirectory("D:\\");

            var folderManagementViewModel = AutoSubstitute.Resolve<FolderManagementViewModel>();
            folderManagementViewModel.Activator.Activate();
        }
    }
}