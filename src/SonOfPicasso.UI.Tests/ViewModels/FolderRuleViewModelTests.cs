using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using FluentAssertions;
using NSubstitute;
using SonOfPicasso.Core.Interfaces;
using SonOfPicasso.Data.Model;
using SonOfPicasso.Testing.Common.Extensions;
using SonOfPicasso.UI.Interfaces;
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

            var manageFolderRulesViewModel = AutoSubstitute.Resolve<IManageFolderRulesViewModel>();

            folderRuleViewModel.Initialize(manageFolderRulesViewModel, directoryInfo,
                new Dictionary<string, FolderRuleActionEnum>(), FolderRuleActionEnum.Remove, new Subject<FolderRuleViewModel>());

            folderRuleViewModel.Name.Should().Be(directoryInfo.Name);
            folderRuleViewModel.Path.Should().Be(directoryInfo.FullName);
            folderRuleViewModel.FolderRuleAction.Should().Be(FolderRuleActionEnum.Remove);
        }

        [Fact]
        public void ShouldInitializeAlways()
        {
            var folderRuleViewModel = AutoSubstitute.Resolve<FolderRuleViewModel>();

            var testPath = Faker.System.DirectoryPathWindows();
            MockFileSystem.AddDirectory(testPath);
            var directoryInfo = MockFileSystem.DirectoryInfo.FromDirectoryName(testPath);

            var manageFolderRulesViewModel = AutoSubstitute.Resolve<IManageFolderRulesViewModel>();
            
            folderRuleViewModel.Initialize(manageFolderRulesViewModel, directoryInfo,
                new Dictionary<string, FolderRuleActionEnum>
                {
                    {testPath, FolderRuleActionEnum.Always}
                }, FolderRuleActionEnum.Remove, new Subject<FolderRuleViewModel>());

            folderRuleViewModel.Name.Should().Be(directoryInfo.Name);
            folderRuleViewModel.Path.Should().Be(directoryInfo.FullName);
            folderRuleViewModel.FolderRuleAction.Should().Be(FolderRuleActionEnum.Always);
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
                .Distinct()
                .Select(word => MockFileSystem.Path.Combine(testPath, word))
                .ToArray();

            foreach (var path in paths) MockFileSystem.AddDirectory(path);

            var directoryInfo = MockFileSystem.DirectoryInfo.FromDirectoryName(testPath);

            var manageFolderRulesViewModel = AutoSubstitute.Resolve<IManageFolderRulesViewModel>();
        
            folderRuleViewModel.Initialize(manageFolderRulesViewModel, directoryInfo,
                new Dictionary<string, FolderRuleActionEnum>
                {
                    {testPath, FolderRuleActionEnum.Always}
                }, FolderRuleActionEnum.Remove, new Subject<FolderRuleViewModel>());

            folderRuleViewModel.FolderRuleAction.Should().Be(FolderRuleActionEnum.Always);

            folderRuleViewModel.Activator.Activate();
            TestSchedulerProvider.TaskPool.AdvanceBy(1);
            TestSchedulerProvider.MainThreadScheduler.AdvanceBy(1);

            folderRuleViewModel.Children.Should().HaveCount(paths.Length);
            folderRuleViewModel.Children.Distinct().Should().HaveCount(paths.Length);
            ((IFolderRuleInput) folderRuleViewModel).Children.Should().HaveCount(paths.Length);

            foreach (var childFolderRuleViewModel in folderRuleViewModel.Children)
            {
                childFolderRuleViewModel.Activator.Activate();
                TestSchedulerProvider.TaskPool.AdvanceBy(1);
                TestSchedulerProvider.MainThreadScheduler.AdvanceBy(1);
            }

            foreach (var childFolderRuleViewModel in folderRuleViewModel.Children)
            {
                childFolderRuleViewModel.FolderRuleAction.Should().Be(FolderRuleActionEnum.Remove);
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
                .Distinct()
                .Select(word => MockFileSystem.Path.Combine(testPath, word))
                .ToArray();

            foreach (var path in paths) MockFileSystem.AddDirectory(path);

            var directoryInfo = MockFileSystem.DirectoryInfo.FromDirectoryName(testPath);

            var manageFolderRulesViewModel = AutoSubstitute.Resolve<IManageFolderRulesViewModel>();

            folderRuleViewModel.Initialize(manageFolderRulesViewModel, directoryInfo,
                new Dictionary<string, FolderRuleActionEnum>(), FolderRuleActionEnum.Remove, new Subject<FolderRuleViewModel>());

            folderRuleViewModel.FolderRuleAction.Should().Be(FolderRuleActionEnum.Remove);

            folderRuleViewModel.Activator.Activate();
            TestSchedulerProvider.TaskPool.AdvanceBy(1);
            TestSchedulerProvider.MainThreadScheduler.AdvanceBy(1);

            folderRuleViewModel.Children.Should().HaveCount(paths.Length);
            folderRuleViewModel.Children.Distinct().Should().HaveCount(paths.Length);

            foreach (var childFolderRuleViewModel in folderRuleViewModel.Children)
            {
                childFolderRuleViewModel.Activator.Activate();
                TestSchedulerProvider.TaskPool.AdvanceBy(1);
                TestSchedulerProvider.MainThreadScheduler.AdvanceBy(1);
            }

            foreach (var childFolderRuleViewModel in folderRuleViewModel.Children)
            {
                childFolderRuleViewModel.FolderRuleAction.Should().Be(FolderRuleActionEnum.Remove);
            }

            folderRuleViewModel.FolderRuleAction = FolderRuleActionEnum.Always;
            TestSchedulerProvider.MainThreadScheduler.AdvanceBy(1);

            foreach (var childFolderRuleViewModel in folderRuleViewModel.Children)
            {
                childFolderRuleViewModel.FolderRuleAction.Should().Be(FolderRuleActionEnum.Always);
            }
        }
    }
}