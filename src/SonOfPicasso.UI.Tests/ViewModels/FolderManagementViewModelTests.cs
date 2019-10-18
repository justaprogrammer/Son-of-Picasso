using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using FluentAssertions;
using NSubstitute;
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
            MockFileSystem.AddDirectory("C:\\$Recycle Bin");
            MockFileSystem.AddDirectory("C:\\" + Faker.Random.Word());
            MockFileSystem.AddDirectory("D:\\");
            MockFileSystem.AddDirectory("D:\\" + Faker.Random.Word());

            var directoryInfoPermissionsService = AutoSubstitute.Resolve<IDirectoryInfoPermissionsService>();
            directoryInfoPermissionsService.IsReadable(Arg.Any<IDirectoryInfo>())
                .ReturnsForAnyArgs(true);

            var folderManagementViewModel = AutoSubstitute.Resolve<FolderManagementViewModel>();
            folderManagementViewModel.Activator.Activate();

            TestSchedulerProvider.TaskPool.AdvanceBy(5);
            TestSchedulerProvider.MainThreadScheduler.AdvanceBy(3);

            folderManagementViewModel.Folders.Count.Should().Be(2);
            foreach (var folderViewModel in folderManagementViewModel.Folders)
            {
//                folderViewModel.Children.Count.Should().Be(1);
            }

            folderManagementViewModel._foldersSourceCache.Count.Should().Be(4);
        }
    }
}