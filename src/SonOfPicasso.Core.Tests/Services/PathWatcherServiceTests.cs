using System;
using System.IO;
using System.IO.Abstractions;
using FluentAssertions;
using NSubstitute;
using SonOfPicasso.Core.Services;
using SonOfPicasso.Testing.Common;
using SonOfPicasso.Testing.Common.Extensions;
using Xunit;
using Xunit.Abstractions;

namespace SonOfPicasso.Core.Tests.Services
{
    public class PathWatcherServiceTests : UnitTestsBase, IDisposable
    {
        public PathWatcherServiceTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }

        [Fact]
        public void ShouldObserveCreationEvents()
        {
            var fileSystemWatcher = Substitute.For<IFileSystemWatcher>();
            FileSystemWatcherQueue.Enqueue(fileSystemWatcher);

            var directoryPathWindows = Faker.System.DirectoryPathWindows();
            MockFileSystem.AddDirectory(directoryPathWindows);

            var pathWatcherService = AutoSubstitute.Resolve<PathWatcherService>();
            pathWatcherService.WatchPath(directoryPathWindows);

            FileSystemEventArgs lastEventArgs = null;
            pathWatcherService.Events.Subscribe(args =>
            {
                lastEventArgs = args;
                AutoResetEvent.Set();
            });

            var fileName = Faker.System.FileName("jpg");

            var createdEventArgs = new FileSystemEventArgs(WatcherChangeTypes.Created, directoryPathWindows, fileName);
            fileSystemWatcher.Created += Raise.Event<FileSystemEventHandler>(fileSystemWatcher, createdEventArgs);

            WaitOne();
            lastEventArgs.Should().Be(createdEventArgs);

            var deletedEventArgs = new FileSystemEventArgs(WatcherChangeTypes.Deleted, directoryPathWindows, fileName);
            fileSystemWatcher.Deleted += Raise.Event<FileSystemEventHandler>(fileSystemWatcher, deletedEventArgs);

            WaitOne();
            lastEventArgs.Should().Be(deletedEventArgs);

            var changedEventArgs = new FileSystemEventArgs(WatcherChangeTypes.Changed, directoryPathWindows, fileName);
            fileSystemWatcher.Changed += Raise.Event<FileSystemEventHandler>(fileSystemWatcher, changedEventArgs);

            WaitOne();
            lastEventArgs.Should().Be(changedEventArgs);

            var renamedEventArgs = new RenamedEventArgs(WatcherChangeTypes.Renamed, directoryPathWindows, fileName,
                MockFileSystem.Path.Combine(Faker.System.DirectoryPathWindows(), Faker.System.FileName("jpg")));
            fileSystemWatcher.Renamed += Raise.Event<RenamedEventHandler>(fileSystemWatcher, renamedEventArgs);

            WaitOne();
            lastEventArgs.Should().Be(renamedEventArgs);
        }
    }
}