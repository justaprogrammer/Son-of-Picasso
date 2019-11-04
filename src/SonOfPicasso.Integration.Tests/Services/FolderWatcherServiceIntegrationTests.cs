using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using Autofac;
using AutofacSerilogIntegration;
using SonOfPicasso.Core.Scheduling;
using SonOfPicasso.Core.Services;
using SonOfPicasso.Data.Model;
using SonOfPicasso.UI.Services;
using Xunit;
using Xunit.Abstractions;

namespace SonOfPicasso.Integration.Tests.Services
{
    public class FolderWatcherServiceIntegrationTests : IntegrationTestsBase, IDisposable
    {
        public FolderWatcherServiceIntegrationTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterLogger();
            containerBuilder.RegisterInstance(FileSystem).As<IFileSystem>();
            containerBuilder.RegisterType<SchedulerProvider>().As<ISchedulerProvider>();
            containerBuilder.RegisterType<FolderWatcherService>();
            _container = containerBuilder.Build();
        }

        private readonly IContainer _container;

        [Fact]
        public void ShouldWatchFileMove()
        {
            var folderPath1 = FileSystem.Path.Combine(TestPath, "folder1");
            var folderPath2 = FileSystem.Path.Combine(TestPath, "folder2");

            FileSystem.Directory.CreateDirectory(folderPath1);
            FileSystem.Directory.CreateDirectory(folderPath2);

            var eventsList = new List<FileSystemEventArgs>();

            var testFilePath1 = FileSystem.Path.Combine(folderPath1, "Hello.txt");
            var testFilePath2 = FileSystem.Path.Combine(folderPath2, "Hello.txt");

            using (var streamWriter = FileSystem.File.CreateText(testFilePath1))
            {
                streamWriter.WriteLine("Hello World!");
                streamWriter.Flush();
            }

            var folderWatcherService = _container.Resolve<FolderWatcherService>();
            using var disposable = folderWatcherService.WatchFolders(new[]
            {
                new FolderRule
                {
                    Path = TestPath,
                    Action = FolderRuleActionEnum.Always
                }
            }).Subscribe(fileSystemEventArgs =>
            {
                eventsList.Add(fileSystemEventArgs);
                AutoResetEvent.Set();
            });

            FileSystem.File.Move(testFilePath1, testFilePath2);

            WaitOne();
        }

        [Fact]
        public void ShouldWatchFileRename()
        {
            var folderPath = FileSystem.Path.Combine(TestPath, "folder1");

            FileSystem.Directory.CreateDirectory(folderPath);

            var eventsList = new List<FileSystemEventArgs>();

            var testFilePath1 = FileSystem.Path.Combine(folderPath, "Hello.txt");
            var testFilePath2 = FileSystem.Path.Combine(folderPath, "Hello1.txt");

            using (var streamWriter = FileSystem.File.CreateText(testFilePath1))
            {
                streamWriter.WriteLine("Hello World!");
                streamWriter.Flush();
            }

            var folderWatcherService = _container.Resolve<FolderWatcherService>();
            using var disposable = folderWatcherService.WatchFolders(new[]
            {
                new FolderRule
                {
                    Path = TestPath,
                    Action = FolderRuleActionEnum.Always
                }
            }).Subscribe(fileSystemEventArgs =>
            {
                eventsList.Add(fileSystemEventArgs);
                AutoResetEvent.Set();
            });

            FileSystem.File.Move(testFilePath1, testFilePath2);

            WaitOne();
        }
    }
}