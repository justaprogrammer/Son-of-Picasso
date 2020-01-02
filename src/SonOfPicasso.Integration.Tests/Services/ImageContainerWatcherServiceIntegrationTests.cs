using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Autofac;
using DynamicData;
using FluentAssertions;
using FluentAssertions.Execution;
using Serilog;
using SonOfPicasso.Core.Interfaces;
using SonOfPicasso.Core.Model;
using SonOfPicasso.Core.Services;
using SonOfPicasso.Data.Model;
using SonOfPicasso.Testing.Common;
using Xunit;
using Xunit.Abstractions;

namespace SonOfPicasso.Integration.Tests.Services
{
    public class ImageContainerWatcherServiceIntegrationTests : IntegrationTestsBase
    {
        public ImageContainerWatcherServiceIntegrationTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
            var containerBuilder = GetContainerBuilder();

            containerBuilder.RegisterType<ImageContainerWatcherService>();
            containerBuilder.RegisterType<ImageLocationService>()
                .As<IImageLocationService>();
            containerBuilder.RegisterType<FolderRulesManagementService>()
                .As<IFolderRulesManagementService>();

            Container = containerBuilder.Build();
        }

        protected override IContainer Container { get; }

        private ImageRef CreateImageRef(string imagePath)
        {
            return new ImageRef(Faker.Random.Int(), imagePath, Faker.Date.Recent(), Faker.Date.Recent(), Faker.Date.Recent(), Faker.Random.Int(), Faker.Random.String(), Faker.PickRandom<ImageContainerTypeEnum>(), Faker.Date.Recent());
        }

        [Fact]
        public async Task ShouldStartWithNoExistingRules()
        {
            await InitializeDataContextAsync();

            var imageRefCache = new SourceCache<ImageRef, string>(imageRef => imageRef.ImagePath);
            var imageContainerWatcherService = Container.Resolve<ImageContainerWatcherService>();
            
            imageContainerWatcherService
                .Start(imageRefCache)
                .Subscribe(unit => { }, () =>
                {
                    AutoResetEvent.Set();
                });

            WaitOne(5);
        }

        [Fact]
        public async Task ShouldDetectCreatedOrChangedFiles()
        {
            await InitializeDataContextAsync();

            var folderRulesManagementService = Container.Resolve<IFolderRulesManagementService>();
            await folderRulesManagementService.ResetFolderManagementRules(new[]
            {
                new FolderRule
                {
                    Path = ImagesPath,
                    Action = FolderRuleActionEnum.Always
                }
            });

            var imageRefCache = new SourceCache<ImageRef, string>(imageRef => imageRef.ImagePath);
            var imageContainerWatcherService = Container.Resolve<ImageContainerWatcherService>();

            var list = new List<string>();
            imageContainerWatcherService.FileDiscovered.Subscribe(info =>
            {
                list.Add(info);
                Set();
            });

            await imageContainerWatcherService.Start(imageRefCache);
            AutoResetEvent.WaitOne(TimeSpan.FromSeconds(3));

            await GenerateImagesAsync(1);

            WaitOne(45);

            list.Should().HaveCount(1);

            imageContainerWatcherService.Stop();
        }

        [Fact]
        public async Task ShouldDetectUpdatedFiles()
        {
            await InitializeDataContextAsync();

            var folderRulesManagementService = Container.Resolve<IFolderRulesManagementService>();
            await folderRulesManagementService.ResetFolderManagementRules(new[]
            {
                new FolderRule
                {
                    Path = ImagesPath,
                    Action = FolderRuleActionEnum.Always
                }
            });

            var imageRefCache = new SourceCache<ImageRef, string>(imageRef => imageRef.ImagePath);
            var imageContainerWatcherService = Container.Resolve<ImageContainerWatcherService>();

            var list = new List<string>();
            imageContainerWatcherService.FileDiscovered.Subscribe(info =>
            {
                list.Add(info);
                Set();
            });

            await imageContainerWatcherService.Start(imageRefCache);

            var generatedImages = await GenerateImagesAsync(1);
            var path = generatedImages.First().Value.First();

            WaitOne(45);
            
            using(new AssertionScope())
            {
                list.Should().HaveCount(1);
                list.First().Should().Be(path);
            }

            await ImageGenerationService.GenerateImage(path,
                Fakers.ExifDataFaker);

            WaitOne(45);

            using(new AssertionScope())
            {
                list.Should().HaveCount(2);
                list.Skip(1).First().Should().Be(path);
            }

            imageContainerWatcherService.Stop();
        }

        [Fact]
        public async Task ShouldDetectUnknownCreatedOrChangedFiles()
        {
            await InitializeDataContextAsync();

            var folderRulesManagementService = Container.Resolve<IFolderRulesManagementService>();
            await folderRulesManagementService.ResetFolderManagementRules(new[]
            {
                new FolderRule
                {
                    Path = ImagesPath,
                    Action = FolderRuleActionEnum.Always
                }
            });

            var imageRefCache = new SourceCache<ImageRef, string>(imageRef => imageRef.ImagePath);

            var generatedImages = await GenerateImagesAsync(1);
            imageRefCache.AddOrUpdate(CreateImageRef(generatedImages.First().Value.First()));

            var imageContainerWatcherService = Container.Resolve<ImageContainerWatcherService>();

            var list = new List<string>();
            imageContainerWatcherService.FileDiscovered.Subscribe(info =>
            {
                list.Add(info);
                Set();
            });

            await imageContainerWatcherService.Start(imageRefCache);
            await GenerateImagesAsync(1);

            WaitOne(45);

            list.Should().HaveCount(1);
        }

        [Fact]
        public async Task ShouldDetectFileDelete()
        {
            await InitializeDataContextAsync();

            var folderRulesManagementService = Container.Resolve<IFolderRulesManagementService>();
            await folderRulesManagementService.ResetFolderManagementRules(new[]
            {
                new FolderRule
                {
                    Path = ImagesPath,
                    Action = FolderRuleActionEnum.Always
                }
            });

            var generatedImages = await GenerateImagesAsync(1, ImagesPath);
            var path = generatedImages.First().Value.First();

            var imageRefCache = new SourceCache<ImageRef, string>(imageRef => imageRef.ImagePath);
            imageRefCache.AddOrUpdate(CreateImageRef(path));

            var imageContainerWatcherService = Container.Resolve<ImageContainerWatcherService>();

            var list = new List<string>();
            imageContainerWatcherService.FileDeleted.Subscribe(info =>
            {
                list.Add(info);
                Set();
            });

            await imageContainerWatcherService.Start(imageRefCache);
            AutoResetEvent.WaitOne(TimeSpan.FromSeconds(3));

            Logger.Verbose("Delete Path {Path}", path);
            FileSystem.File.Delete(path);

            WaitOne(5);

            list.Should().HaveCount(1);
            list.First().Should().Be(path);
        }

        [Fact]
        public async Task ShouldIgnoreUnknownFileDelete()
        {
            await InitializeDataContextAsync();

            var folderRulesManagementService = Container.Resolve<IFolderRulesManagementService>();
            await folderRulesManagementService.ResetFolderManagementRules(new[]
            {
                new FolderRule
                {
                    Path = ImagesPath,
                    Action = FolderRuleActionEnum.Always
                }
            });

            var generatedImages = await GenerateImagesAsync(1, ImagesPath);
            var path = generatedImages.First().Value.First();

            var imageRefCache = new SourceCache<ImageRef, string>(imageRef => imageRef.ImagePath);

            var imageContainerWatcherService = Container.Resolve<ImageContainerWatcherService>();

            var list = new List<string>();
            imageContainerWatcherService.FileDeleted.Subscribe(info =>
            {
                list.Add(info);
                Set();
            });

            await imageContainerWatcherService.Start(imageRefCache);
            AutoResetEvent.WaitOne(TimeSpan.FromSeconds(1));

            Logger.Verbose("Delete Path {Path}", path);
            FileSystem.File.Delete(path);

            AutoResetEvent.WaitOne(TimeSpan.FromSeconds(5)).Should().BeFalse();

            list.Should().BeEmpty();
        }

        [Fact]
        public async Task ShouldDetectFileRename()
        {
            await InitializeDataContextAsync();

            var folderRulesManagementService = Container.Resolve<IFolderRulesManagementService>();
            await folderRulesManagementService.ResetFolderManagementRules(new[]
            {
                new FolderRule
                {
                    Path = ImagesPath,
                    Action = FolderRuleActionEnum.Always
                }
            });

            var generatedImages = await GenerateImagesAsync(1, ImagesPath);
            var imageRefCache = new SourceCache<ImageRef, string>(imageRef => imageRef.ImagePath);
            var imagePath = generatedImages.First().Value.First();
            imageRefCache.AddOrUpdate(CreateImageRef(imagePath));

            var imageContainerWatcherService = Container.Resolve<ImageContainerWatcherService>();

            var list = new List<(string oldFullPath, string fullPath)>();
            imageContainerWatcherService.FileRenamed.Subscribe(tuple =>
            {
                list.Add(tuple);
                Set();
            });

            await imageContainerWatcherService.Start(imageRefCache);
            AutoResetEvent.WaitOne(TimeSpan.FromSeconds(1));

            var movedTo = Path.Combine(generatedImages.First().Key, "a" + FileSystem.Path.GetFileName(imagePath));
            FileSystem.File.Move(imagePath, movedTo);

            Logger.Verbose("Moving File {Path} {ToPath}", imagePath, movedTo);

            WaitOne(5);

            list.Should().HaveCount(1);
            list.First().Should().Be((imagePath, movedTo));
        }

        [Fact]
        public async Task ShouldDetectUnknownFileRenameAsDiscover()
        {
            await InitializeDataContextAsync();

            var folderRulesManagementService = Container.Resolve<IFolderRulesManagementService>();
            await folderRulesManagementService.ResetFolderManagementRules(new[]
            {
                new FolderRule
                {
                    Path = ImagesPath,
                    Action = FolderRuleActionEnum.Always
                }
            });

            var generatedImages = await GenerateImagesAsync(1, ImagesPath);
            var imageRefCache = new SourceCache<ImageRef, string>(imageRef => imageRef.ImagePath);

            var imageContainerWatcherService = Container.Resolve<ImageContainerWatcherService>();

            var list = new List<string>();
            imageContainerWatcherService.FileDiscovered.Subscribe(info =>
            {
                list.Add(info);
                Set();
            });

            await imageContainerWatcherService.Start(imageRefCache);
            AutoResetEvent.WaitOne(TimeSpan.FromSeconds(1));
     
            var file = generatedImages.First().Value.First();
            var movedTo = Path.Combine(generatedImages.First().Key, "a" + FileSystem.Path.GetFileName(file));
            FileSystem.File.Move(file, movedTo);

            WaitOne(5);
            list.Should().HaveCount(1);
            list.First().Should().Be(movedTo);
        }

        [Fact]
        public async Task ShouldDetectDirectoryCreate()
        {
            await InitializeDataContextAsync();

            var folderRulesManagementService = Container.Resolve<IFolderRulesManagementService>();
            await folderRulesManagementService.ResetFolderManagementRules(new[]
            {
                new FolderRule
                {
                    Path = ImagesPath,
                    Action = FolderRuleActionEnum.Always
                }
            });

            var imageRefCache = new SourceCache<ImageRef, string>(imageRef => imageRef.ImagePath);
            var imageContainerWatcherService = Container.Resolve<ImageContainerWatcherService>();
           
            await imageContainerWatcherService.Start(imageRefCache);
            AutoResetEvent.WaitOne(TimeSpan.FromSeconds(1));
         
            var directoryInfo = ImagesDirectoryInfo.CreateSubdirectory("Test");

            AutoResetEvent.WaitOne(TimeSpan.FromSeconds(3));
        }

        [Fact]
        public async Task ShouldDetectDirectoryDelete()
        {
            await InitializeDataContextAsync();

            var folderRulesManagementService = Container.Resolve<IFolderRulesManagementService>();
            await folderRulesManagementService.ResetFolderManagementRules(new[]
            {
                new FolderRule
                {
                    Path = ImagesPath,
                    Action = FolderRuleActionEnum.Always
                }
            });

            var subdirectory = ImagesDirectoryInfo.CreateSubdirectory("Test");

            var imageRefCache = new SourceCache<ImageRef, string>(imageRef => imageRef.ImagePath);
            var imageContainerWatcherService = Container.Resolve<ImageContainerWatcherService>();
        
            await imageContainerWatcherService.Start(imageRefCache);
            AutoResetEvent.WaitOne(TimeSpan.FromSeconds(1));
       
            subdirectory.Delete();

            AutoResetEvent.WaitOne(TimeSpan.FromSeconds(3));
        }

        [Fact]
        public async Task ShouldDetectDirectoryRename()
        {
            await InitializeDataContextAsync();

            var folderRulesManagementService = Container.Resolve<IFolderRulesManagementService>();
            await folderRulesManagementService.ResetFolderManagementRules(new[]
            {
                new FolderRule
                {
                    Path = ImagesPath,
                    Action = FolderRuleActionEnum.Always
                }
            });

            var subdirectory = ImagesDirectoryInfo.CreateSubdirectory("Test");

            var imageRefCache = new SourceCache<ImageRef, string>(imageRef => imageRef.ImagePath);
            var imageContainerWatcherService = Container.Resolve<ImageContainerWatcherService>();
        
            await imageContainerWatcherService.Start(imageRefCache);
            AutoResetEvent.WaitOne(TimeSpan.FromSeconds(1));
         
            subdirectory.MoveTo(FileSystem.Path.Combine(ImagesPath, "Test2"));

            AutoResetEvent.WaitOne(TimeSpan.FromSeconds(3));
        }
    }
}