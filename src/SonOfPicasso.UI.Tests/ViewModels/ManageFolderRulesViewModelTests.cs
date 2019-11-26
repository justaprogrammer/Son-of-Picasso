using System.IO;
using System.IO.Abstractions;
using NSubstitute;
using Xunit.Abstractions;

namespace SonOfPicasso.UI.Tests.ViewModels
{
    public class  ManageFolderRulesViewModelTests : ViewModelTestsBase
    {
        public ManageFolderRulesViewModelTests(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
            MockFileSystem.AddDirectory("C:\\");
            MockFileSystem.AddDirectory("D:\\");
            MockFileSystem.AddDirectory("G:\\");

            var driveInfoFactory = AutoSubstitute.Resolve<IDriveInfoFactory>();
            var cDrive = Substitute.For<IDriveInfo>();
            cDrive.DriveType.Returns(DriveType.Fixed);
            cDrive.IsReady.Returns(true);
            cDrive.RootDirectory.Returns(MockFileSystem.DirectoryInfo.FromDirectoryName("C:\\"));

            var dDrive = Substitute.For<IDriveInfo>();
            dDrive.DriveType.Returns(DriveType.CDRom);
            dDrive.IsReady.Returns(true);

            var gDrive = Substitute.For<IDriveInfo>();
            dDrive.DriveType.Returns(DriveType.Network);
            dDrive.IsReady.Returns(true);

            driveInfoFactory.GetDrives().ReturnsForAnyArgs(new[]
            {
                cDrive,
                dDrive,
                gDrive
            });
        }
    }
}