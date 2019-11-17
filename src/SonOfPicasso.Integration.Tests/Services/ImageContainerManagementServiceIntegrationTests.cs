using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Autofac;
using Dapper;
using DynamicData;
using DynamicData.Binding;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.EntityFrameworkCore;
using SonOfPicasso.Core.Interfaces;
using SonOfPicasso.Core.Model;
using SonOfPicasso.Core.Services;
using SonOfPicasso.Data.Model;
using Xunit;
using Xunit.Abstractions;

namespace SonOfPicasso.Integration.Tests.Services
{
    public class ImageContainerManagementServiceIntegrationTests : IntegrationTestsBase
    {
        public ImageContainerManagementServiceIntegrationTests(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
            var containerBuilder = GetContainerBuilder();
            containerBuilder.RegisterType<ExifDataService>().As<IExifDataService>();
            containerBuilder.RegisterType<ImageLocationService>().As<IImageLocationService>();
            containerBuilder.RegisterType<ImageContainerManagementService>();
            containerBuilder.RegisterType<FolderRulesManagementService>().As<IFolderRulesManagementService>();
            containerBuilder.RegisterType<ImageContainerOperationService>().As<IImageContainerOperationService>();
            containerBuilder.RegisterType<FolderWatcherService>().As<IFolderWatcherService>();

            Container = containerBuilder.Build();
        }

        protected override IContainer Container { get; }

        [Fact(Skip = "Broken")]
        public async Task ShouldScanExistingFolder()
        {
            await InitializeDataContextAsync();
            await using var connection = DataContext.Database.GetDbConnection();

            var imageCount = 50;
            var generateImagesAsync = await GenerateImagesAsync(imageCount);

            var imagesDirectoryInfo = FileSystem.DirectoryInfo.FromDirectoryName(ImagesPath);
            var directoryCount = imagesDirectoryInfo.EnumerateDirectories().Count();

            var imageContainerManagementService = Container.Resolve<ImageContainerManagementService>();

            var imageContainers = new ObservableCollectionExtended<IImageContainer>();
            imageContainerManagementService.ImageContainerCache
                .Connect()
                .ObserveOn(SchedulerProvider.TaskPool)
                .Bind(imageContainers)
                .Subscribe();

            var imageRefs = new ObservableCollectionExtended<ImageRef>();
            imageContainerManagementService.AlbumImageRefCache
                .Connect()
                .ObserveOn(SchedulerProvider.TaskPool)
                .Bind(imageRefs)
                .Subscribe();

            await imageContainerManagementService.Start();
            await imageContainerManagementService.ScanFolder(ImagesPath);

            imageContainers.WhenPropertyChanged(items => items.Count)
                .CombineLatest(imageRefs.WhenPropertyChanged(items => items.Count),(pV1, pV2) => (pV1.Value, pV2.Value))
                .Subscribe(tuple =>
                {
                    if (tuple.Item1 == generateImagesAsync.Count && tuple.Item2 == imageCount)
                    {
                        AutoResetEvent.Set();
                    }
                });

            WaitOne(TimeSpan.FromSeconds(10));

            var images = (await connection.QueryAsync<Image>("SELECT * FROM Images"))
                .ToArray();

            var folders = (await connection.QueryAsync<Folder>("SELECT * FROM Folders"))
                .ToArray();

            var exifDatas = (await connection.QueryAsync("SELECT * FROM ExifData"))
                .ToArray();

            var folderRules = (await connection.QueryAsync<FolderRule>("SELECT * FROM FolderRules"))
                .ToArray();

            using (new AssertionScope())
            {
                images.Should().HaveCount(imageCount);
                folders.Should().HaveCount(generateImagesAsync.Count);
                exifDatas.Should().HaveCount(imageCount);
                folderRules.Should().HaveCount(1);

                var folderRule = folderRules.First();
                folderRule.Path.Should().Be(ImagesPath);
                folderRule.Action.Should().Be(FolderRuleActionEnum.Once);

                imageContainers.Should().HaveCount(generateImagesAsync.Count);
                imageRefs.Should().HaveCount(imageCount);
            }
        }

        [Fact(Skip = "Broken")]
        public async Task ShouldWatchFolderWithAlwaysRulePreset()
        {
            await InitializeDataContextAsync();

            var folderRulesManagementService = Container.Resolve<IFolderRulesManagementService>();
            await folderRulesManagementService.AddFolderManagementRule(new FolderRule
            {
                Action = FolderRuleActionEnum.Always,
                Path = ImagesPath
            });

            var imageContainerManagementService = Container.Resolve<ImageContainerManagementService>();

            var imageContainers = new ObservableCollectionExtended<IImageContainer>();
            imageContainerManagementService.ImageContainerCache
                .Connect()
                .ObserveOn(SchedulerProvider.TaskPool)
                .Bind(imageContainers)
                .Subscribe();

            var imageRefs = new ObservableCollectionExtended<ImageRef>();
            imageContainerManagementService.AlbumImageRefCache
                .Connect()
                .ObserveOn(SchedulerProvider.TaskPool)
                .Bind(imageRefs)
                .Subscribe();

            await imageContainerManagementService.Start();

            var imageCount = 20;
            var generateImagesAsync = await GenerateImagesAsync(imageCount);

            imageContainers.WhenPropertyChanged(items => items.Count)
                .CombineLatest(imageRefs.WhenPropertyChanged(items => items.Count),(pV1, pV2) => (pV1.Value, pV2.Value))
                .Subscribe(tuple =>
                {
                    if (tuple.Item1 == generateImagesAsync.Count && tuple.Item2 == imageCount)
                    {
                        AutoResetEvent.Set();
                    }
                });

            WaitOne(TimeSpan.FromSeconds(60));

            await using var connection = DataContext.Database.GetDbConnection();

            var images = (await connection.QueryAsync<Image>("SELECT * FROM Images"))
                .ToArray();

            var folders = (await connection.QueryAsync<Folder>("SELECT * FROM Folders"))
                .ToArray();

            var exifDatas = (await connection.QueryAsync("SELECT * FROM ExifData"))
                .ToArray();

            var folderRules = (await connection.QueryAsync<FolderRule>("SELECT * FROM FolderRules"))
                .ToArray();

            using (new AssertionScope())
            {
                images.Should().HaveCount(imageCount);
                folders.Should().HaveCount(generateImagesAsync.Count);
                exifDatas.Should().HaveCount(imageCount);
                folderRules.Should().HaveCount(1);

                var folderRule = folderRules.First();
                folderRule.Path.Should().Be(ImagesPath);
                folderRule.Action.Should().Be(FolderRuleActionEnum.Always);

                imageContainers.Should().HaveCount(generateImagesAsync.Count);
                imageRefs.Should().HaveCount(imageCount);
            }
        }

        [Fact(Skip = "Broken")]
        public async Task ShouldWatchFolderWithResetAlwaysRule()
        {
            await InitializeDataContextAsync();

            var imageContainerManagementService = Container.Resolve<ImageContainerManagementService>();

            var imageContainers = new ObservableCollectionExtended<IImageContainer>();
            imageContainerManagementService.ImageContainerCache
                .Connect()
                .ObserveOn(SchedulerProvider.TaskPool)
                .Bind(imageContainers)
                .Subscribe();

            var imageRefs = new ObservableCollectionExtended<ImageRef>();
            imageContainerManagementService.AlbumImageRefCache
                .Connect()
                .ObserveOn(SchedulerProvider.TaskPool)
                .Bind(imageRefs)
                .Subscribe();

            await imageContainerManagementService.Start();
            await imageContainerManagementService.ResetRules(new[]
                {new FolderRule {Path = ImagesPath, Action = FolderRuleActionEnum.Always}});

            var imageCount = 1;
            var generateImagesAsync = await GenerateImagesAsync(imageCount);

            imageContainers.WhenPropertyChanged(items => items.Count)
                .CombineLatest(imageRefs.WhenPropertyChanged(items => items.Count),(pV1, pV2) => (pV1.Value, pV2.Value))
                .Subscribe(tuple =>
                {
                    if (tuple.Item1 == generateImagesAsync.Count && tuple.Item2 == imageCount)
                    {
                        AutoResetEvent.Set();
                    }
                });

            WaitOne(TimeSpan.FromSeconds(5));

            await using var connection = DataContext.Database.GetDbConnection();

            var images = (await connection.QueryAsync<Image>("SELECT * FROM Images"))
                .ToArray();

            var folders = (await connection.QueryAsync<Folder>("SELECT * FROM Folders"))
                .ToArray();

            var exifDatas = (await connection.QueryAsync("SELECT * FROM ExifData"))
                .ToArray();

            var folderRules = (await connection.QueryAsync<FolderRule>("SELECT * FROM FolderRules"))
                .ToArray();

            using (new AssertionScope())
            {
                images.Should().HaveCount(imageCount);
                folders.Should().HaveCount(generateImagesAsync.Count);
                exifDatas.Should().HaveCount(imageCount);
                folderRules.Should().HaveCount(1);

                var folderRule = folderRules.First();
                folderRule.Path.Should().Be(ImagesPath);
                folderRule.Action.Should().Be(FolderRuleActionEnum.Always);

                imageContainers.Should().HaveCount(generateImagesAsync.Count);
                imageRefs.Should().HaveCount(imageCount);
            }
        }
    }
}