using FluentAssertions;
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
            MockFileSystem.AddDirectory("C:\\");
            MockFileSystem.AddDirectory("C:\\" + Faker.Random.Word());
            MockFileSystem.AddDirectory("D:\\");
            MockFileSystem.AddDirectory("D:\\" + Faker.Random.Word());

            var folderManagementViewModel = AutoSubstitute.Resolve<FolderManagementViewModel>();
            folderManagementViewModel.Activator.Activate();

            TestSchedulerProvider.TaskPool.AdvanceBy(1);
            TestSchedulerProvider.MainThreadScheduler.AdvanceBy(3);

            folderManagementViewModel.Folders.Count.Should().Be(2);
            foreach (var folderViewModel in folderManagementViewModel.Folders)
            {
                folderViewModel.Children.Count.Should().Be(1);
            }

            folderManagementViewModel._foldersSourceCache.Count.Should().Be(4);
        }
    }
}