using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using Autofac;
using DynamicData.Binding;
using FluentAssertions;
using SonOfPicasso.Core.Services;
using SonOfPicasso.Data.Model;
using Xunit;
using Xunit.Abstractions;

namespace SonOfPicasso.Integration.Tests.Services
{
    public class FolderWatcherServiceIntegrationTests : IntegrationTestsBase
    {
        public FolderWatcherServiceIntegrationTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
            var containerBuilder = GetContainerBuilder();
            containerBuilder.RegisterType<FolderWatcherService>();

            Container = containerBuilder.Build();
        }

        protected override IContainer Container { get; }

        [Fact]
        public void ShouldWatchFileMove()
        {
            var folderPath1 = FileSystem.Path.Combine(TestPath, "folder1");
            var folderPath2 = FileSystem.Path.Combine(TestPath, "folder2");

            FileSystem.Directory.CreateDirectory(folderPath1);
            FileSystem.Directory.CreateDirectory(folderPath2);


            var testFilePath1 = FileSystem.Path.Combine(folderPath1, "Hello.txt");
            var testFilePath2 = FileSystem.Path.Combine(folderPath2, "Hello.txt");

            using (var streamWriter = FileSystem.File.CreateText(testFilePath1))
            {
                streamWriter.WriteLine("Hello World!");
                streamWriter.Flush();
                streamWriter.Close();
            }

            var eventsList = new List<FileSystemEventArgs>();
            var folderWatcherService = Container.Resolve<FolderWatcherService>();
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

            WaitOne(TimeSpan.FromSeconds(5));
        }

        [Fact]
        public void ShouldWatchFileCreate()
        {
            var eventsList = new ObservableCollectionExtended<FileSystemEventArgs>();

            var folderWatcherService = Container.Resolve<FolderWatcherService>();
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
            });

            var files = Faker.MakeLazy(10, () => FileSystem.Path.Combine(TestPath, Faker.System.FileName("txt")))
                .Distinct()
                .ToList();

            eventsList.WhenPropertyChanged(e => e.Count)
                .Subscribe(propertyValue =>
                {
                    if (propertyValue.Value == 20)
                    {
                        AutoResetEvent.Set();
                    }
                });

            files
                .ToObservable()
                .ObserveOn(SchedulerProvider.TaskPool)
                .Subscribe(s =>
                {
                    using var streamWriter = FileSystem.File.CreateText(s);
                    streamWriter.WriteLine("Hello World!");
                    streamWriter.Flush();
                    streamWriter.Close();
                });

            WaitOne(TimeSpan.FromSeconds(15));
        }

        [Fact]
        public void ShouldWatchFileCreateWithFilter()
        {
            var eventsList = new ObservableCollectionExtended<FileSystemEventArgs>();

            var folderWatcherService = Container.Resolve<FolderWatcherService>();
            using var disposable = folderWatcherService.WatchFolders(new[]
            {
                new FolderRule
                {
                    Path = TestPath,
                    Action = FolderRuleActionEnum.Always
                }
            }, new[] { ".jpg" }).Subscribe(fileSystemEventArgs =>
              {
                  eventsList.Add(fileSystemEventArgs);
              });

            var txtFiles = Faker.MakeLazy(10, () => FileSystem.Path.Combine(TestPath, Faker.System.FileName("txt")))
                .Distinct()
                .ToArray();

            var jpgFiles = Faker.MakeLazy(10, () => FileSystem.Path.Combine(TestPath, Faker.System.FileName("jpg")))
                .Distinct()
                .ToArray();

            IList<string> allFiles = txtFiles.Concat(jpgFiles)
                .ToArray();

            allFiles = Faker.Random.ListItems(allFiles, allFiles.Count);

            eventsList.WhenPropertyChanged(e => e.Count)
                .Subscribe(propertyValue =>
                {
                    if (propertyValue.Value == jpgFiles.Length * 2)
                    {
                        AutoResetEvent.Set();
                    }
                });

            allFiles
                .ToObservable()
                .ObserveOn(SchedulerProvider.TaskPool)
                .Subscribe(s =>
                {
                    using var streamWriter = FileSystem.File.CreateText(s);
                    streamWriter.WriteLine("Hello World!");
                    streamWriter.Flush();
                    streamWriter.Close();
                });          
            
            WaitOne(TimeSpan.FromSeconds(15));
        }

        [Fact]
        public void ShouldWatchFileRename()
        {
            var folderPath = FileSystem.Path.Combine(TestPath, "folder1");

            FileSystem.Directory.CreateDirectory(folderPath);

            var testFilePath1 = FileSystem.Path.Combine(folderPath, "Hello.txt");
            var testFilePath2 = FileSystem.Path.Combine(folderPath, "Hello1.txt");

            using (var streamWriter = FileSystem.File.CreateText(testFilePath1))
            {
                streamWriter.WriteLine("Hello World!");
                streamWriter.Flush();
                streamWriter.Close();
            }

            var eventsList = new ObservableCollectionExtended<FileSystemEventArgs>();
            var folderWatcherService = Container.Resolve<FolderWatcherService>();
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
            });

            eventsList.WhenPropertyChanged(e => e.Count)
                .Subscribe(propertyValue =>
                {
                    if (propertyValue.Value == 1)
                    {
                        AutoResetEvent.Set();
                    }
                });

            FileSystem.File.Move(testFilePath1, testFilePath2);

            WaitOne(TimeSpan.FromSeconds(15));
        }
    }
}