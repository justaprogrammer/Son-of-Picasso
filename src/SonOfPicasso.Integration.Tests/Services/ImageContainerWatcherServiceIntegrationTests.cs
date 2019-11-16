using System;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Autofac;
using DynamicData;
using SonOfPicasso.Core.Interfaces;
using SonOfPicasso.Core.Model;
using SonOfPicasso.Core.Services;
using SonOfPicasso.Data.Model;
using SQLitePCL;
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
            containerBuilder.RegisterType<FolderRulesManagementService>()
                .As<IFolderRulesManagementService>();

            Container = containerBuilder.Build();
        }

        protected override IContainer Container { get; }

        [Fact]
        public async Task ShouldDetectFileCreate()
        {
            Logger.Verbose("Running Test {Name}", nameof(ShouldDetectFileCreate));

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

            var imageRefCache = new SourceCache<ImageRef, string>(imageRef => imageRef.Key);
            var imageContainerWatcherService = Container.Resolve<ImageContainerWatcherService>();
            await imageContainerWatcherService.Start(imageRefCache);

            await GenerateImagesAsync(10);

            AutoResetEvent.WaitOne(TimeSpan.FromSeconds(10));
            Logger.Verbose("Complete Test {Name}", nameof(ShouldDetectFileCreate));
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

            var dictionary = await GenerateImagesAsync(10);
      
            var imageRefCache = new SourceCache<ImageRef, string>(imageRef => imageRef.Key);
            var imageContainerWatcherService = Container.Resolve<ImageContainerWatcherService>();
            await imageContainerWatcherService.Start(imageRefCache);

            var first = dictionary.First().Value.First();
            
            Logger.Verbose("Delete File {Path}", first);
            FileSystem.File.Delete(first);

            AutoResetEvent.WaitOne(TimeSpan.FromSeconds(10));
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

            var dictionary = await GenerateImagesAsync(10);
            
            var imageRefCache = new SourceCache<ImageRef, string>(imageRef => imageRef.Key);
            var imageContainerWatcherService = Container.Resolve<ImageContainerWatcherService>();
            await imageContainerWatcherService.Start(imageRefCache);

            var file = dictionary.First().Value.First();
            var movedTo = Path.Combine(dictionary.First().Key, "a" + FileSystem.Path.GetFileName(file));
            FileSystem.File.Move(file, movedTo);

            AutoResetEvent.WaitOne(TimeSpan.FromSeconds(10));
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

            var imageRefCache = new SourceCache<ImageRef, string>(imageRef => imageRef.Key);
            var imageContainerWatcherService = Container.Resolve<ImageContainerWatcherService>();
            await imageContainerWatcherService.Start(imageRefCache);

            AutoResetEvent.WaitOne(TimeSpan.FromSeconds(10));
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

            var dictionary = await GenerateImagesAsync(10);
      
            var imageRefCache = new SourceCache<ImageRef, string>(imageRef => imageRef.Key);
            var imageContainerWatcherService = Container.Resolve<ImageContainerWatcherService>();
            await imageContainerWatcherService.Start(imageRefCache);

            AutoResetEvent.WaitOne(TimeSpan.FromSeconds(10));
            Logger.Verbose("Complete Test {Name}", nameof(ShouldDetectDirectoryDelete));
        }

        [Fact]
        public async Task ShouldDetectDirectoryRename()
        {
            Logger.Verbose("Running Test {Name}", nameof(ShouldDetectDirectoryRename));

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

            var dictionary = await GenerateImagesAsync(10);
            
            var imageRefCache = new SourceCache<ImageRef, string>(imageRef => imageRef.Key);
            var imageContainerWatcherService = Container.Resolve<ImageContainerWatcherService>();
            await imageContainerWatcherService.Start(imageRefCache);

            AutoResetEvent.WaitOne(TimeSpan.FromSeconds(10));
            Logger.Verbose("Complete Test {Name}", nameof(ShouldDetectDirectoryRename));
        }
    }
}