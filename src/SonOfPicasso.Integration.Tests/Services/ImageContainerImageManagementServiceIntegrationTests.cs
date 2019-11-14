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
    public class ImageContainerImageManagementServiceIntegrationTests : IntegrationTestsBase
    {
        public ImageContainerImageManagementServiceIntegrationTests(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
            var containerBuilder = GetContainerBuilder();
            containerBuilder.RegisterType<ExifDataService>().As<IExifDataService>();
            containerBuilder.RegisterType<ImageLocationService>().As<IImageLocationService>();
            containerBuilder.RegisterType<ImageContainerImageManagementService>();
            containerBuilder.RegisterType<FolderRulesManagementService>().As<IFolderRulesManagementService>();
            containerBuilder.RegisterType<ImageContainerOperationService>().As<IImageContainerOperationService>();
            containerBuilder.RegisterType<FolderWatcherService>().As<IFolderWatcherService>();

            Container = containerBuilder.Build();
        }

        protected override IContainer Container { get; }

        [Fact]
        public async Task ShouldScanExistingFolder()
        {
            await InitializeDataContextAsync();

            var imageCount = 50;
            var generateImagesAsync = await GenerateImagesAsync(imageCount);

            var connectableImageManagementService = Container.Resolve<ImageContainerImageManagementService>();

            var imageContainers = new ObservableCollectionExtended<IImageContainer>();
            connectableImageManagementService.ImageContainerCache
                .Connect()
                .ObserveOn(SchedulerProvider.TaskPool)
                .Bind(imageContainers)
                .Subscribe();

            var imageRefs = new ObservableCollectionExtended<ImageRef>();
            connectableImageManagementService.ImageRefCache
                .Connect()
                .ObserveOn(SchedulerProvider.TaskPool)
                .Bind(imageRefs)
                .Subscribe();

            await connectableImageManagementService.Start();
            await connectableImageManagementService.ScanFolder(ImagesPath);

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
        }

        [Fact]
        public async Task ShouldWatchFolder()
        {
            await InitializeDataContextAsync();

            var folderRulesManagementService = Container.Resolve<IFolderRulesManagementService>();
            await folderRulesManagementService.AddFolderManagementRule(new FolderRule
            {
                Action = FolderRuleActionEnum.Always,
                Path = ImagesPath
            });

            var connectableImageManagementService = Container.Resolve<ImageContainerImageManagementService>();

            var imageContainers = new ObservableCollectionExtended<IImageContainer>();
            connectableImageManagementService.ImageContainerCache
                .Connect()
                .ObserveOn(SchedulerProvider.TaskPool)
                .Bind(imageContainers)
                .Subscribe();

            var imageRefs = new ObservableCollectionExtended<ImageRef>();
            connectableImageManagementService.ImageRefCache
                .Connect()
                .ObserveOn(SchedulerProvider.TaskPool)
                .Bind(imageRefs)
                .Subscribe();

            await connectableImageManagementService.Start();

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

            var folders = (await connection.QueryAsync<Folder>("SELECT * FROM Folders"))
                .ToArray();

            var images = (await connection.QueryAsync<Image>("SELECT * FROM Images"))
                .ToArray();

            using (new AssertionScope())
            {
                folders.Should().HaveCount(generateImagesAsync.Count);
                images.Should().HaveCount(imageCount);

                imageContainers.Should().HaveCount(generateImagesAsync.Count);
                imageRefs.Should().HaveCount(imageCount);
            }
        }
    }
}