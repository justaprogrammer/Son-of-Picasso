using System;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using FluentAssertions;
using NSubstitute;
using SonOfPicasso.Core.Interfaces;
using SonOfPicasso.Data.Model;
using SonOfPicasso.UI.ViewModels;
using Xunit;
using Xunit.Abstractions;

namespace SonOfPicasso.UI.Tests.ViewModels
{
    public class ManageFolderRulesViewModelTests : ViewModelTestsBase
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

        [Fact]
        public void ShouldInitializeAndActivate()
        {
            var rootPath = "C:\\";

            var paths = Faker.Random.WordsArray(2)
                .Select(s => MockFileSystem.Path.Combine(rootPath, s))
                .Select(s => MockFileSystem.DirectoryInfo.FromDirectoryName(s))
                .ToArray();

            foreach (var path in paths)
                MockFileSystem.AddDirectory(path.FullName);

            var folderRulesManagementService = AutoSubstitute.Resolve<IFolderRulesManagementService>();
            folderRulesManagementService.GetFolderManagementRules()
                .Returns(Observable.Return(new[]
                {
                    new FolderRule
                    {
                        Path = paths.First().FullName,
                        Action = FolderRuleActionEnum.Always
                    }
                }));

            var directoryInfoPermissionsService = AutoSubstitute.Resolve<IDirectoryInfoPermissionsService>();
            directoryInfoPermissionsService.IsReadable(Arg.Any<IDirectoryInfo>())
                .ReturnsForAnyArgs(true);

            var manageFolderRulesViewModel = AutoSubstitute.Resolve<ManageFolderRulesViewModel>();
            manageFolderRulesViewModel.Activator.Activate();

            TestSchedulerProvider.TaskPool.AdvanceBy(1);
            TestSchedulerProvider.MainThreadScheduler.AdvanceBy(1);

            manageFolderRulesViewModel.Folders.Count.Should().Be(1);

            manageFolderRulesViewModel.Folders[0].Activator.Activate();

            TestSchedulerProvider.TaskPool.AdvanceBy(1);
            TestSchedulerProvider.MainThreadScheduler.AdvanceBy(1);

            manageFolderRulesViewModel.Folders[0].Children.Count.Should().Be(2);

            manageFolderRulesViewModel.Folders[0].Children[0].Activator.Activate();
            TestSchedulerProvider.TaskPool.AdvanceBy(1);
            TestSchedulerProvider.MainThreadScheduler.AdvanceBy(1);

            manageFolderRulesViewModel.Folders[0].Children[1].Activator.Activate();
            TestSchedulerProvider.TaskPool.AdvanceBy(1);
            TestSchedulerProvider.MainThreadScheduler.AdvanceBy(1);

            manageFolderRulesViewModel.Folders[0].Name.Should().Be(String.Empty);
            manageFolderRulesViewModel.Folders[0].Path.Should().Be(rootPath);

            manageFolderRulesViewModel.Folders[0].Children[0].Name.Should().Be(paths[0].Name);
            manageFolderRulesViewModel.Folders[0].Children[0].Path.Should().Be(paths[0].FullName);
            manageFolderRulesViewModel.Folders[0].Children[0].Children.Count.Should().Be(0);

            manageFolderRulesViewModel.Folders[0].Children[1].Name.Should().Be(paths[1].Name);
            manageFolderRulesViewModel.Folders[0].Children[1].Path.Should().Be(paths[1].FullName);
            manageFolderRulesViewModel.Folders[0].Children[1].Children.Count.Should().Be(0);
        }

        [Fact]
        public void ShouldCancel()
        {
            var rootPath = "C:\\";

            var paths = Faker.Random.WordsArray(2)
                .Select(s => MockFileSystem.Path.Combine(rootPath, s))
                .Select(s => MockFileSystem.DirectoryInfo.FromDirectoryName(s))
                .ToArray();

            foreach (var path in paths)
                MockFileSystem.AddDirectory(path.FullName);

            var folderRulesManagementService = AutoSubstitute.Resolve<IFolderRulesManagementService>();
            folderRulesManagementService.GetFolderManagementRules()
                .Returns(Observable.Return(new[]
                {
                    new FolderRule
                    {
                        Path = paths.First().FullName,
                        Action = FolderRuleActionEnum.Always
                    }
                }));

            var directoryInfoPermissionsService = AutoSubstitute.Resolve<IDirectoryInfoPermissionsService>();
            directoryInfoPermissionsService.IsReadable(Arg.Any<IDirectoryInfo>())
                .ReturnsForAnyArgs(true);

            var manageFolderRulesViewModel = AutoSubstitute.Resolve<ManageFolderRulesViewModel>();
            manageFolderRulesViewModel.Activator.Activate();

            TestSchedulerProvider.TaskPool.AdvanceBy(1);
            TestSchedulerProvider.MainThreadScheduler.AdvanceBy(1);

            manageFolderRulesViewModel.Folders[0].Activator.Activate();

            TestSchedulerProvider.TaskPool.AdvanceBy(1);
            TestSchedulerProvider.MainThreadScheduler.AdvanceBy(1);

            manageFolderRulesViewModel.Folders[0].Children[0].Activator.Activate();
            TestSchedulerProvider.TaskPool.AdvanceBy(1);
            TestSchedulerProvider.MainThreadScheduler.AdvanceBy(1);

            manageFolderRulesViewModel.Folders[0].Children[1].Activator.Activate();
            TestSchedulerProvider.TaskPool.AdvanceBy(1);
            TestSchedulerProvider.MainThreadScheduler.AdvanceBy(1);

            manageFolderRulesViewModel.CancelInteraction.RegisterHandler(context =>
            {
                context.SetOutput(Unit.Default);
            });

            manageFolderRulesViewModel.Cancel.Execute(Unit.Default)
                .Subscribe(unit =>
                {
                    AutoResetEvent.Set();
                });

            TestSchedulerProvider.TaskPool.AdvanceBy(1);
            WaitOne();
        }

        [Fact]
        public void ShouldContinue()
        {
            var rootPath = "C:\\";

            var paths = Faker.Random.WordsArray(2)
                .Select(s => MockFileSystem.Path.Combine(rootPath, s))
                .Select(s => MockFileSystem.DirectoryInfo.FromDirectoryName(s))
                .ToArray();

            foreach (var path in paths)
                MockFileSystem.AddDirectory(path.FullName);

            var folderRulesManagementService = AutoSubstitute.Resolve<IFolderRulesManagementService>();
            folderRulesManagementService.GetFolderManagementRules()
                .Returns(Observable.Return(new[]
                {
                    new FolderRule
                    {
                        Path = paths.First().FullName,
                        Action = FolderRuleActionEnum.Always
                    }
                }));

            var directoryInfoPermissionsService = AutoSubstitute.Resolve<IDirectoryInfoPermissionsService>();
            directoryInfoPermissionsService.IsReadable(Arg.Any<IDirectoryInfo>())
                .ReturnsForAnyArgs(true);

            var manageFolderRulesViewModel = AutoSubstitute.Resolve<ManageFolderRulesViewModel>();
            manageFolderRulesViewModel.Activator.Activate();

            TestSchedulerProvider.TaskPool.AdvanceBy(1);
            TestSchedulerProvider.MainThreadScheduler.AdvanceBy(1);

            manageFolderRulesViewModel.Folders[0].Activator.Activate();

            TestSchedulerProvider.TaskPool.AdvanceBy(1);
            TestSchedulerProvider.MainThreadScheduler.AdvanceBy(1);

            manageFolderRulesViewModel.Folders[0].Children[0].Activator.Activate();
            TestSchedulerProvider.TaskPool.AdvanceBy(1);
            TestSchedulerProvider.MainThreadScheduler.AdvanceBy(1);

            manageFolderRulesViewModel.Folders[0].Children[1].Activator.Activate();
            TestSchedulerProvider.TaskPool.AdvanceBy(1);
            TestSchedulerProvider.MainThreadScheduler.AdvanceBy(1);

            manageFolderRulesViewModel.ContinueInteraction.RegisterHandler(context =>
            {
                context.SetOutput(Unit.Default);
            });

            manageFolderRulesViewModel.Continue.Execute(Unit.Default)
                .Subscribe(unit =>
                {
                    AutoResetEvent.Set();
                });

            TestSchedulerProvider.TaskPool.AdvanceBy(1);
            WaitOne();
        }
    }
}