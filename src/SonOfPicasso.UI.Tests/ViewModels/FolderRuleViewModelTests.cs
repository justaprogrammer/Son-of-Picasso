﻿using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NSubstitute;
using SonOfPicasso.Core.Interfaces;
using SonOfPicasso.Data.Model;
using SonOfPicasso.Testing.Common.Extensions;
using SonOfPicasso.UI.ViewModels;
using Xunit;
using Xunit.Abstractions;

namespace SonOfPicasso.UI.Tests.ViewModels
{
    public class FolderRuleViewModelTests : ViewModelTestsBase
    {
        public FolderRuleViewModelTests(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        [Fact]
        public void ShouldInitializeRemoved()
        {
            var folderRuleViewModel = AutoSubstitute.Resolve<FolderRuleViewModel>();

            var testPath = Faker.System.DirectoryPathWindows();
            MockFileSystem.AddDirectory(testPath);
            var directoryInfo = MockFileSystem.DirectoryInfo.FromDirectoryName(testPath);

            folderRuleViewModel.Initialize(directoryInfo,
                new Dictionary<string, FolderRuleActionEnum>());

            folderRuleViewModel.Name.Should().Be(directoryInfo.Name);
            folderRuleViewModel.FullName.Should().Be(directoryInfo.FullName);
            folderRuleViewModel.ManageFolderState.Should().Be(FolderRuleActionEnum.Remove);
        }

        [Fact]
        public void ShouldInitializeAlways()
        {
            var folderRuleViewModel = AutoSubstitute.Resolve<FolderRuleViewModel>();

            var testPath = Faker.System.DirectoryPathWindows();
            MockFileSystem.AddDirectory(testPath);
            var directoryInfo = MockFileSystem.DirectoryInfo.FromDirectoryName(testPath);

            folderRuleViewModel.Initialize(directoryInfo,
                new Dictionary<string, FolderRuleActionEnum>
                {
                    {testPath, FolderRuleActionEnum.Always}
                });

            folderRuleViewModel.Name.Should().Be(directoryInfo.Name);
            folderRuleViewModel.FullName.Should().Be(directoryInfo.FullName);
            folderRuleViewModel.ManageFolderState.Should().Be(FolderRuleActionEnum.Always);
        }

        [Fact]
        public void ShouldActivate()
        {
            var directoryInfoPermissionsService = AutoSubstitute.Resolve<IDirectoryInfoPermissionsService>();
            directoryInfoPermissionsService.IsReadable(default)
                .ReturnsForAnyArgs(true);

            var folderRuleViewModel = AutoSubstitute.Resolve<FolderRuleViewModel>();

            var testPath = Faker.System.DirectoryPathWindows();
            MockFileSystem.AddDirectory(testPath);

            var paths = Faker.Random.WordsArray(Faker.Random.Int(5, 10))
                .Select(word => MockFileSystem.Path.Combine(testPath, word))
                .ToArray();

            foreach (var path in paths) MockFileSystem.AddDirectory(path);

            var directoryInfo = MockFileSystem.DirectoryInfo.FromDirectoryName(testPath);

            folderRuleViewModel.Initialize(directoryInfo,
                new Dictionary<string, FolderRuleActionEnum>
                {
                    {testPath, FolderRuleActionEnum.Always}
                });

            folderRuleViewModel.ManageFolderState.Should().Be(FolderRuleActionEnum.Always);

            folderRuleViewModel.Activator.Activate();
            TestSchedulerProvider.TaskPool.AdvanceBy(1);
            TestSchedulerProvider.MainThreadScheduler.AdvanceBy(1);

            folderRuleViewModel.Children.Count.Should().Be(paths.Length);
            folderRuleViewModel.Children.Distinct().Count().Should().Be(paths.Length);
            ((IFolderRuleInput) folderRuleViewModel).Children.Count.Should().Be(paths.Length);

            foreach (var childFolderRuleViewModel in folderRuleViewModel.Children)
            {
                childFolderRuleViewModel.Activator.Activate();
                TestSchedulerProvider.TaskPool.AdvanceBy(1);
                TestSchedulerProvider.MainThreadScheduler.AdvanceBy(1);
            }

            foreach (var childFolderRuleViewModel in folderRuleViewModel.Children)
            {
                childFolderRuleViewModel.ManageFolderState.Should().Be(FolderRuleActionEnum.Remove);
            }
        }

        [Fact]
        public void ShouldCascadeStateChange()
        {
            var directoryInfoPermissionsService = AutoSubstitute.Resolve<IDirectoryInfoPermissionsService>();
            directoryInfoPermissionsService.IsReadable(default)
                .ReturnsForAnyArgs(true);

            var folderRuleViewModel = AutoSubstitute.Resolve<FolderRuleViewModel>();

            var testPath = Faker.System.DirectoryPathWindows();
            MockFileSystem.AddDirectory(testPath);

            var paths = Faker.Random.WordsArray(Faker.Random.Int(5, 10))
                .Select(word => MockFileSystem.Path.Combine(testPath, word))
                .ToArray();

            foreach (var path in paths) MockFileSystem.AddDirectory(path);

            var directoryInfo = MockFileSystem.DirectoryInfo.FromDirectoryName(testPath);

            folderRuleViewModel.Initialize(directoryInfo,
                new Dictionary<string, FolderRuleActionEnum>());

            folderRuleViewModel.ManageFolderState.Should().Be(FolderRuleActionEnum.Remove);

            folderRuleViewModel.Activator.Activate();
            TestSchedulerProvider.TaskPool.AdvanceBy(1);
            TestSchedulerProvider.MainThreadScheduler.AdvanceBy(1);

            folderRuleViewModel.Children.Count.Should().Be(paths.Length);
            folderRuleViewModel.Children.Distinct().Count().Should().Be(paths.Length);

            foreach (var childFolderRuleViewModel in folderRuleViewModel.Children)
            {
                childFolderRuleViewModel.Activator.Activate();
                TestSchedulerProvider.TaskPool.AdvanceBy(1);
                TestSchedulerProvider.MainThreadScheduler.AdvanceBy(1);
            }

            foreach (var childFolderRuleViewModel in folderRuleViewModel.Children)
            {
                childFolderRuleViewModel.ManageFolderState.Should().Be(FolderRuleActionEnum.Remove);
            }

            folderRuleViewModel.ManageFolderState = FolderRuleActionEnum.Always;
            TestSchedulerProvider.MainThreadScheduler.AdvanceBy(1);

            foreach (var childFolderRuleViewModel in folderRuleViewModel.Children)
            {
                childFolderRuleViewModel.ManageFolderState.Should().Be(FolderRuleActionEnum.Always);
            }
        }
    }
}