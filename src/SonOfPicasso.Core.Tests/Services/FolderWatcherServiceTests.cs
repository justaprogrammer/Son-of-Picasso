using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using FluentAssertions;
using NSubstitute;
using NSubstitute.Core.Events;
using SonOfPicasso.Core.Services;
using SonOfPicasso.Data.Model;
using SonOfPicasso.Testing.Common;
using Xunit;
using Xunit.Abstractions;

namespace SonOfPicasso.Core.Tests.Services
{
    public class FolderWatcherServiceTests : UnitTestsBase
    {
        public FolderWatcherServiceTests(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        [Fact]
        public void ShouldEvalSingleRule()
        {
            var path = "C:\\Hello";

            var folderRules = new[]
            {
                    new FolderRule
                    {
                        Path = path,
                        Action = FolderRuleActionEnum.Always
                    }
                };

            var fileSystemWatcher = Substitute.For<IFileSystemWatcher>();
            FileSystemWatcherQueue.Enqueue(fileSystemWatcher);

            FileSystemEventArgs lastArgs = null;
            var folderWatcherService = AutoSubstitute.Resolve<FolderWatcherService>();
            folderWatcherService.WatchFolders(folderRules)
                .Subscribe(args =>
                {
                    lastArgs = args;
                    AutoResetEvent.Set();
                });

            FileSystemWatcherFactory.ReceivedWithAnyArgs(1)
                .FromPath(default);

            FileSystemWatcherFactory.Received(1)
                .FromPath(path);

            MockFileSystem.AddFile(MockFileSystem.Path.Combine(path, "hello.txt"), new MockFileData("Hello World!"));

            fileSystemWatcher.Created += 
                Raise.Event<FileSystemEventHandler>(this, 
                    new FileSystemEventArgs(WatcherChangeTypes.Created, path, "hello.txt"));
         
            TestSchedulerProvider.TaskPool.AdvanceBy(1);

            WaitOne();
        }

        [Fact]
        public void ShouldEvalRulesWithOneExclude()
        {
            var rootPath = "C:\\Hello";
            var excludePath = $"{rootPath}\\World";

            var folderRules = new[]
            {
                new FolderRule
                {
                    Path = rootPath,
                    Action = FolderRuleActionEnum.Always
                },
                new FolderRule
                {
                    Path = excludePath,
                    Action = FolderRuleActionEnum.Remove
                }
            };

            var fileSystemWatcher1 = Substitute.For<IFileSystemWatcher>();
            FileSystemWatcherQueue.Enqueue(fileSystemWatcher1);

            var fileSystemWatcher2 = Substitute.For<IFileSystemWatcher>();
            FileSystemWatcherQueue.Enqueue(fileSystemWatcher2);

            FileSystemEventArgs lastArgs = null;
            var folderWatcherService = AutoSubstitute.Resolve<FolderWatcherService>();
            folderWatcherService.WatchFolders(folderRules)
                .Subscribe(args =>
                {
                    lastArgs = args;
                    AutoResetEvent.Set();
                });

            FileSystemWatcherFactory.ReceivedWithAnyArgs(1)
                .FromPath(default);

            FileSystemWatcherFactory.Received(1)
                .FromPath(rootPath);

            MockFileSystem.AddFile(MockFileSystem.Path.Combine(rootPath, "hello.txt"), new MockFileData("Hello World!"));
            MockFileSystem.AddFile(MockFileSystem.Path.Combine(excludePath, "hello.txt"), new MockFileData("Hello World!"));

            fileSystemWatcher1.Created +=
                Raise.Event<FileSystemEventHandler>(this,
                    new FileSystemEventArgs(WatcherChangeTypes.Created, rootPath, "hello.txt"));

            TestSchedulerProvider.TaskPool.AdvanceBy(1);

            WaitOne();

            fileSystemWatcher1.Created +=
                Raise.Event<FileSystemEventHandler>(this,
                    new FileSystemEventArgs(WatcherChangeTypes.Created, excludePath, "hello.txt"));

            TestSchedulerProvider.TaskPool.AdvanceBy(1);

            AutoResetEvent.WaitOne(500).Should().BeFalse();
        }

        [Fact]
        public void ShouldEvalRulesWithExcludeAndInclude()
        {
            var rootPath = "C:\\Hello";
            var excludePath = $"{rootPath}\\World";
            var includePath = $"{excludePath}\\Hello";

            var folderRules = new[]
            {
                new FolderRule
                {
                    Path = rootPath,
                    Action = FolderRuleActionEnum.Always
                },
                new FolderRule
                {
                    Path = excludePath,
                    Action = FolderRuleActionEnum.Remove
                },
                new FolderRule
                {
                    Path = includePath,
                    Action = FolderRuleActionEnum.Always
                }
            };

            var fileSystemWatcher1 = Substitute.For<IFileSystemWatcher>();
            FileSystemWatcherQueue.Enqueue(fileSystemWatcher1);

            var fileSystemWatcher2 = Substitute.For<IFileSystemWatcher>();
            FileSystemWatcherQueue.Enqueue(fileSystemWatcher2);

            FileSystemEventArgs lastArgs = null;
            var folderWatcherService = AutoSubstitute.Resolve<FolderWatcherService>();
            folderWatcherService.WatchFolders(folderRules)
                .Subscribe(args =>
                {
                    lastArgs = args;
                    AutoResetEvent.Set();
                });

            FileSystemWatcherFactory.ReceivedWithAnyArgs(1)
                .FromPath(default);

            FileSystemWatcherFactory.Received(1)
                .FromPath(rootPath);

            MockFileSystem.AddFile(MockFileSystem.Path.Combine(rootPath, "hello.txt"), new MockFileData("Hello World!"));
            MockFileSystem.AddFile(MockFileSystem.Path.Combine(includePath, "hello.txt"), new MockFileData("Hello World!"));

            fileSystemWatcher1.Created +=
                Raise.Event<FileSystemEventHandler>(this,
                    new FileSystemEventArgs(WatcherChangeTypes.Created, rootPath, "hello.txt"));

            TestSchedulerProvider.TaskPool.AdvanceBy(1);

            WaitOne();

            fileSystemWatcher1.Created +=
                Raise.Event<FileSystemEventHandler>(this,
                    new FileSystemEventArgs(WatcherChangeTypes.Created, includePath, "hello.txt"));

            TestSchedulerProvider.TaskPool.AdvanceBy(1);

            WaitOne();
        }

        [Fact]
        public void ShouldEvalTwoRules()
        {
            var path1 = "C:\\Hello";
            var path2 = "C:\\World";

            var folderRules = new[]
            {
                new FolderRule
                {
                    Path = path1,
                    Action = FolderRuleActionEnum.Always
                },
                new FolderRule
                {
                    Path = path2,
                    Action = FolderRuleActionEnum.Always
                }
            };

            var fileSystemWatcher1 = Substitute.For<IFileSystemWatcher>();
            var fileSystemWatcher2 = Substitute.For<IFileSystemWatcher>();
            FileSystemWatcherQueue.Enqueue(fileSystemWatcher1);
            FileSystemWatcherQueue.Enqueue(fileSystemWatcher2);

            FileSystemEventArgs lastArgs = null;
            var folderWatcherService = AutoSubstitute.Resolve<FolderWatcherService>();
            folderWatcherService.WatchFolders(folderRules)
                .Subscribe(args =>
                {
                    lastArgs = args;
                    AutoResetEvent.Set();
                });

            FileSystemWatcherFactory.ReceivedWithAnyArgs(2)
                .FromPath(default);

            FileSystemWatcherFactory.Received(1)
                .FromPath(path1);

            FileSystemWatcherFactory.Received(1)
                .FromPath(path2);

            MockFileSystem.AddFile(MockFileSystem.Path.Combine(path1, "hello.txt"), new MockFileData("Hello World!"));
            MockFileSystem.AddFile(MockFileSystem.Path.Combine(path2, "hello.txt"), new MockFileData("Hello World!"));

            fileSystemWatcher1.Created +=
                Raise.Event<FileSystemEventHandler>(this,
                    new FileSystemEventArgs(WatcherChangeTypes.Created, path1, "hello.txt"));

            TestSchedulerProvider.TaskPool.AdvanceBy(1);

            WaitOne();

            fileSystemWatcher2.Created +=
                Raise.Event<FileSystemEventHandler>(this,
                    new FileSystemEventArgs(WatcherChangeTypes.Created, path2, "hello.txt"));

            TestSchedulerProvider.TaskPool.AdvanceBy(1);

            WaitOne();
        }

        [Fact]
        public void ShouldHandleInternalRename()
        {
            var path = "C:\\Hello";

            var folderRules = new[]
            {
                new FolderRule
                {
                    Path = path,
                    Action = FolderRuleActionEnum.Always
                }
            };

            var fileSystemWatcher1 = Substitute.For<IFileSystemWatcher>();
            FileSystemWatcherQueue.Enqueue(fileSystemWatcher1);

            FileSystemEventArgs lastArgs = null;
            var folderWatcherService = AutoSubstitute.Resolve<FolderWatcherService>();
            folderWatcherService.WatchFolders(folderRules)
                .Subscribe(args =>
                {
                    lastArgs = args;
                    AutoResetEvent.Set();
                });

            FileSystemWatcherFactory.ReceivedWithAnyArgs(1)
                .FromPath(default);

            FileSystemWatcherFactory.Received(1)
                .FromPath(path);

            MockFileSystem.AddFile(MockFileSystem.Path.Combine(path, "hello1.txt"), new MockFileData("Hello World!"));

            fileSystemWatcher1.Renamed +=
                Raise.Event<RenamedEventHandler>(this,
                    new RenamedEventArgs(WatcherChangeTypes.Renamed, path, "hello1.txt", "hello.txt"));

            TestSchedulerProvider.TaskPool.AdvanceBy(1);

            WaitOne();
        }
    }
}
