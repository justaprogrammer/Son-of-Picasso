using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
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
            _fileSystemWatcherQueue = new Queue<IFileSystemWatcher>();
            _fileSystemWatcherFactory = AutoSubstitute.Resolve<IFileSystemWatcherFactory>();
            _fileSystemWatcherFactory.FromPath(Arg.Any<string>())
                .ReturnsForAnyArgs(info => _fileSystemWatcherQueue.Dequeue());
        }

        private readonly Queue<IFileSystemWatcher> _fileSystemWatcherQueue;
        private readonly IFileSystemWatcherFactory _fileSystemWatcherFactory;

        [Fact]
        public void ShouldEvalSimpleRule()
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
            _fileSystemWatcherQueue.Enqueue(fileSystemWatcher);

            var folderWatcherService = AutoSubstitute.Resolve<FolderWatcherService>();
            folderWatcherService.StartWatch(folderRules);

            _fileSystemWatcherFactory.Received(1)
                .FromPath(path);

            fileSystemWatcher.Created += Raise.Event<FileSystemEventHandler>(this, new FileSystemEventArgs(WatcherChangeTypes.Created, path, ""));
        }

        [Fact]
        public void ShouldEvalRuleWithOneExclude()
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

            var fileSystemWatcher = Substitute.For<IFileSystemWatcher>();
            _fileSystemWatcherQueue.Enqueue(fileSystemWatcher);

            var folderWatcherService = AutoSubstitute.Resolve<FolderWatcherService>();
            folderWatcherService.StartWatch(folderRules);

            _fileSystemWatcherFactory.Received(1)
                .FromPath(rootPath);
        }
    }
}
