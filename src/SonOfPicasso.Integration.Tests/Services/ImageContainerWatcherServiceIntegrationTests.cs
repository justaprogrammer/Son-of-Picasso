using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Autofac;
using DynamicData;
using FluentAssertions;
using SonOfPicasso.Core.Interfaces;
using SonOfPicasso.Core.Model;
using SonOfPicasso.Core.Services;
using SonOfPicasso.Data.Model;
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
            var imageRef = new ImageRef(Faker.Random.Int(), Faker.Random.String(), imagePath,
                Faker.Date.Recent(), Faker.Random.String(), Faker.PickRandom<ImageContainerTypeEnum>(),
                Faker.Date.Recent());
            return imageRef;
        }

        [Fact]
        public async Task ShouldStartWithNoExistingRules()
        {
            Logger.Verbose("Running Test {Name}", nameof(ShouldStartWithNoExistingRules));

            await InitializeDataContextAsync();

            var imageRefCache = new SourceCache<ImageRef, string>(imageRef => imageRef.ImagePath);
            var imageContainerWatcherService = Container.Resolve<ImageContainerWatcherService>();
            imageContainerWatcherService.Start(imageRefCache).Subscribe(unit => { }, () => { AutoResetEvent.Set(); });

            WaitOne();
            Logger.Verbose("Complete Test {Name}", nameof(ShouldStartWithNoExistingRules));
        }

        [Fact]
        public async Task ShouldOnlyDetectCreatedOrChangedFiles()
        {
            Logger.Verbose("Running Test {Name}", nameof(ShouldOnlyDetectCreatedOrChangedFiles));

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

            await GenerateImagesAsync(1);

            WaitOne(5);

            list.Should().HaveCount(1);

            imageContainerWatcherService.Stop();

            Logger.Verbose("Complete Test {Name}", nameof(ShouldOnlyDetectCreatedOrChangedFiles));
        }

        [Fact]
        public async Task ShouldOnlyDetectUnknownCreatedOrChangedFiles()
        {
            Logger.Verbose("Running Test {Name}", nameof(ShouldOnlyDetectUnknownCreatedOrChangedFiles));

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

            var generatedImages2 = await GenerateImagesAsync(1);

            WaitOne(5);

            list.Should().HaveCount(1);

            Logger.Verbose("Complete Test {Name}", nameof(ShouldOnlyDetectUnknownCreatedOrChangedFiles));
        }

        [Fact]
        public async Task ShouldDetectFileDelete()
        {
            Logger.Verbose("Running Test {Name}", nameof(ShouldDetectFileDelete));

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

            WaitOne(15);

            list.Should().HaveCount(1);
            list.First().Should().Be(path);

            Logger.Verbose("Complete Test {Name}", nameof(ShouldDetectFileDelete));
        }

        [Fact]
        public async Task ShouldDetectFileRename()
        {
            Logger.Verbose("Running Test {Name}", nameof(ShouldDetectFileRename));

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
            imageRefCache.AddOrUpdate(CreateImageRef(generatedImages.First().Value.First()));

            var imageContainerWatcherService = Container.Resolve<ImageContainerWatcherService>();
            await imageContainerWatcherService.Start(imageRefCache);

            var file = generatedImages.First().Value.First();
            var movedTo = Path.Combine(generatedImages.First().Key, "a" + FileSystem.Path.GetFileName(file));
            FileSystem.File.Move(file, movedTo);

            AutoResetEvent.WaitOne(TimeSpan.FromSeconds(5));
            Logger.Verbose("Complete Test {Name}", nameof(ShouldDetectFileRename));
        }

        [Fact]
        public async Task ShouldDetectDirectoryCreate()
        {
            Logger.Verbose("Running Test {Name}", nameof(ShouldDetectDirectoryCreate));

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

            ImagesDirectoryInfo.CreateSubdirectory("Test");

            AutoResetEvent.WaitOne(TimeSpan.FromSeconds(5));
            Logger.Verbose("Complete Test {Name}", nameof(ShouldDetectDirectoryCreate));
        }

        [Fact]
        public async Task ShouldDetectDirectoryDelete()
        {
            Logger.Verbose("Running Test {Name}", nameof(ShouldDetectDirectoryDelete));

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

            subdirectory.Delete();

            AutoResetEvent.WaitOne(TimeSpan.FromSeconds(5));
            Logger.Verbose("Complete Test {Name}", nameof(ShouldDetectDirectoryDelete));
        }

        [Fact]
        public async Task ShouldDetectDirectoryRename()
        {
            Logger.Verbose("Running Test {Name}", nameof(ShouldDetectDirectoryDelete));

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

            subdirectory.MoveTo(FileSystem.Path.Combine(ImagesPath, "Test2"));

            AutoResetEvent.WaitOne(TimeSpan.FromSeconds(5));
            Logger.Verbose("Complete Test {Name}", nameof(ShouldDetectDirectoryDelete));
        }
    }
}