﻿using System;
using System.IO.Abstractions;
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
using Serilog;
using Serilog.Filters;
using PicasaDatabaseReader.Core;
using PicasaDatabaseReader.Core.Interfaces;
using SonOfPicasso.Core.Interfaces;
using SonOfPicasso.Core.Model;
using SonOfPicasso.Core.Scheduling;
using SonOfPicasso.Core.Services;
using SonOfPicasso.Data.Model;
using SonOfPicasso.Testing.Common.Scheduling;
using Xunit;
using Xunit.Abstractions;

namespace SonOfPicasso.Integration.Tests.Services
{
    public class ImageContainerManagementServiceIntegrationTests : IntegrationTestsBase
    {
        public IDirectoryInfo ThumbnailsDirectoryInfo { get; }

        public ImageContainerManagementServiceIntegrationTests(ITestOutputHelper testOutputHelper)
            : base(GetCustomConfiguration(testOutputHelper))
        {
            ThumbnailsDirectoryInfo = ImagesDirectoryInfo.Parent.CreateSubdirectory("Thumbnails");
            
            var containerBuilder = GetContainerBuilder();
            containerBuilder.RegisterType<ExifDataService>().As<IExifDataService>();
            containerBuilder.RegisterType<TestBlobCacheProvider>().As<IBlobCacheProvider>();

            containerBuilder.Register(context =>
            {
                var logger = context.Resolve<ILogger>().ForContext<ImageLoadingService>();

                return new ImageLoadingService(context.Resolve<IFileSystem>(), logger,
                    context.Resolve<ISchedulerProvider>(), context.Resolve<IBlobCacheProvider>(),
                    ThumbnailsDirectoryInfo.FullName);
            }).As<IImageLoadingService>();

            containerBuilder.RegisterType<ImageLocationService>().As<IImageLocationService>();
            containerBuilder.RegisterType<ImageContainerManagementService>();
            containerBuilder.RegisterType<FolderRulesManagementService>().As<IFolderRulesManagementService>();
            containerBuilder.RegisterType<ImageContainerOperationService>().As<IImageContainerOperationService>();
            containerBuilder.RegisterType<ImageContainerWatcherService>().As<IImageContainerWatcherService>();
            containerBuilder.RegisterInstance(SchedulerProvider).As<PicasaDatabaseReader.Core.Scheduling.ISchedulerProvider>();
            containerBuilder.RegisterType<DatabaseReader>().As<IDatabaseReader>();

            Container = containerBuilder.Build();
        }

        private static LoggerConfiguration GetCustomConfiguration(ITestOutputHelper testOutputHelper)
        {
            return GetLoggerConfiguration(testOutputHelper, configuration => configuration
                .Filter.ByExcluding(Matching.FromSource<ExifDataService>())
                .Filter.ByExcluding(Matching.FromSource<ImageLoadingService>()));
        }

        protected override IContainer Container { get; }

        [SkippableFact]
        public async Task ShouldScanFolderWithRealImages()
        {
            var environmentVariable =
                Environment.GetEnvironmentVariable("SonOfPicasso_IntegrationTests_RealImagesFolder");
            Skip.If(string.IsNullOrWhiteSpace(environmentVariable),
                "RealImagesFolder environment variable not provided");

            Logger.Information("ShouldScanFolderWithRealImages");

            await InitializeDataContextAsync();

            var imageContainerManagementService = Container.Resolve<ImageContainerManagementService>();
            imageContainerManagementService.ImageContainerCache
                .Connect()
                .Subscribe(set =>
                {
                    var names = set.Select(change => change.Current.Name)
                        .ToArray();

                    Logger.Verbose("ImageContainerCache {@Names} Adds:{Adds} Removes:{Removes} Updates:{Updates}",
                        names, set.Adds, set.Removes, set.Updates);
                });

            imageContainerManagementService.FolderImageRefCache
                .Connect()
                .Subscribe(set =>
                {
                    var distinctContainers = set
                        .Select(change => change.Current.ContainerKey)
                        .Distinct()
                        .Count();

                    Logger.Verbose(
                        "FolderImageRefCache Containers:{ContainerCount} Adds:{Adds} Removes:{Removes} Updates:{Updates}",
                        distinctContainers, set.Adds, set.Removes, set.Updates);
                });

            imageContainerManagementService
                .ScanFolder(environmentVariable)
                .SubscribeOn(SchedulerProvider.TaskPool)
                .Subscribe(unit => { }, () => { });

            AutoResetEvent.WaitOne(TimeSpan.FromSeconds(20));
        }

        [Fact]
        public async Task ShouldScanExistingFolder()
        {
            await InitializeDataContextAsync();
            await using var connection = DataContext.Database.GetDbConnection();

            var desiredImageCount = 50;
            var generateImagesAsync = await GenerateImagesAsync(desiredImageCount);
            var imageCount = generateImagesAsync.SelectMany(pair => pair.Value).Count();
            var folderCount = generateImagesAsync.Count;

            imageCount.Should().Be(desiredImageCount);

            var imageContainerManagementService = Container.Resolve<ImageContainerManagementService>();

            var imageContainers = new ObservableCollectionExtended<IImageContainer>();
            imageContainerManagementService.ImageContainerCache
                .Connect()
                .ObserveOn(SchedulerProvider.TaskPool)
                .Bind(imageContainers)
                .Subscribe();

            var imageRefs = new ObservableCollectionExtended<ImageRef>();
            imageContainerManagementService.FolderImageRefCache
                .Connect()
                .ObserveOn(SchedulerProvider.TaskPool)
                .Bind(imageRefs)
                .Subscribe();

            await imageContainerManagementService.Start();

            var beginTimedOperation = Logger.BeginTimedOperation("Started Scanning");

            await imageContainerManagementService.ScanFolder(ImagesPath);

            imageContainers.WhenPropertyChanged(items => items.Count)
                .CombineLatest(imageRefs.WhenPropertyChanged(items => items.Count),
                    (pV1, pV2) => (pV1.Value, pV2.Value))
                .Subscribe(tuple =>
                {
                    Logger.Debug("Folders {FolderCount}/{FolderTotal} Images {ImageCount}/{ImageTotal}",
                        tuple.Item1, folderCount, tuple.Item2, imageCount);

                    if (tuple.Item1 == folderCount && tuple.Item2 == imageCount) AutoResetEvent.Set();
                });

            using (new AssertionScope())
            {
                WaitOne(TimeSpan.FromSeconds(15));

                beginTimedOperation.Dispose();

                var images = (await connection.QueryAsync<Image>("SELECT * FROM Images"))
                    .ToArray();

                var folders = (await connection.QueryAsync<Folder>("SELECT * FROM Folders"))
                    .ToArray();

                var exifDatas = (await connection.QueryAsync("SELECT * FROM ExifData"))
                    .ToArray();

                var folderRules = (await connection.QueryAsync<FolderRule>("SELECT * FROM FolderRules"))
                    .ToArray();

                images.Should().HaveCount(imageCount);
                folders.Should().HaveCount(folderCount);
                exifDatas.Should().HaveCount(imageCount);
                folderRules.Should().HaveCount(1);

                var folderRule = folderRules.First();
                folderRule.Path.Should().Be(ImagesPath);
                folderRule.Action.Should().Be(FolderRuleActionEnum.Once);

                imageContainers.Should().HaveCount(folderCount);
                imageRefs.Should().HaveCount(imageCount);
            }
        }

        [Fact]
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
            imageContainerManagementService.FolderImageRefCache
                .Connect()
                .ObserveOn(SchedulerProvider.TaskPool)
                .Bind(imageRefs)
                .Subscribe();

            await imageContainerManagementService.Start();

            var imageCount = 20;
            var generateImagesAsync = await GenerateImagesAsync(imageCount);

            imageContainers.WhenPropertyChanged(items => items.Count)
                .CombineLatest(imageRefs.WhenPropertyChanged(items => items.Count),
                    (pV1, pV2) => (pV1.Value, pV2.Value))
                .Subscribe(tuple =>
                {
                    if (tuple.Item1 == generateImagesAsync.Count && tuple.Item2 == imageCount) AutoResetEvent.Set();
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

        [Fact]
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
            imageContainerManagementService.FolderImageRefCache
                .Connect()
                .ObserveOn(SchedulerProvider.TaskPool)
                .Bind(imageRefs)
                .Subscribe();

            await imageContainerManagementService.Start();
            await imageContainerManagementService.ResetRules(new[]
                {new FolderRule {Path = ImagesPath, Action = FolderRuleActionEnum.Always}});

            AutoResetEvent.WaitOne(TimeSpan.FromSeconds(1));

            var imageCount = 1;
            var generateImagesAsync = await GenerateImagesAsync(imageCount);

            imageContainers.WhenPropertyChanged(items => items.Count)
                .CombineLatest(imageRefs.WhenPropertyChanged(items => items.Count),
                    (pV1, pV2) => (pV1.Value, pV2.Value))
                .Subscribe(tuple =>
                {
                    if (tuple.Item1 == generateImagesAsync.Count && tuple.Item2 == imageCount) AutoResetEvent.Set();
                });

            WaitOne(TimeSpan.FromSeconds(15));

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

        [SkippableFact]
        public void ShouldScanPicasaDatabase()
        {
            var variable = Environment.GetEnvironmentVariable("SonOfPicasso_IntegrationTest_PicasaDatabsePath");
            Skip.If(string.IsNullOrWhiteSpace(variable), "Skipped if database path not present");
        }
    }
}