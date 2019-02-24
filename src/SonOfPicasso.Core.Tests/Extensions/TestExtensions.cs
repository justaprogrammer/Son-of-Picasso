using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using Akavache;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SonOfPicasso.Core.Interfaces;
using SonOfPicasso.Core.Scheduling;
using SonOfPicasso.Core.Services;
using SonOfPicasso.Testing.Common;
using SonOfPicasso.Testing.Common.Extensions;
using SonOfPicasso.Testing.Common.Scheduling;
using SonOfPicasso.Testing.Common.Services;

namespace SonOfPicasso.Core.Tests.Extensions
{
    public static class TestExtensions
    {
        public static SharedCache CreateSharedCache<T>(this TestsBase<T> tests, IBlobCache blobCache = null)
        {
            var serviceCollection = tests.GetServiceCollection();

            var serviceProvider = serviceCollection
                .BuildServiceProvider();

            blobCache = blobCache ?? new InMemoryBlobCache();

            return new SharedCache(serviceProvider.GetService<ILogger<SharedCache>>(), blobCache);
        }

        public static ImageLocationService CreateImageLocationService<T>(this TestsBase<T> tests,
            IFileSystem fileSystem = null,
            ISchedulerProvider schedulerProvider = null)
        {
            fileSystem = fileSystem ?? new MockFileSystem();
            schedulerProvider = schedulerProvider ?? new TestSchedulerProvider();

            var serviceCollection = tests.GetServiceCollection()
                .AddSingleton(fileSystem)
                .AddSingleton(schedulerProvider)
                .AddSingleton<ImageLocationService>();

            var buildServiceProvider = serviceCollection
                .BuildServiceProvider();

            return buildServiceProvider.MustGetService<ImageLocationService>();
        }

        public static ImageManagementService CreateImageManagementService<T>(this TestsBase<T> tests,
            ISharedCache sharedCache = null,
            IImageLocationService imageLocationService = null)
        {
            imageLocationService = imageLocationService ?? Substitute.For<IImageLocationService>();

            var serviceCollection = tests.GetServiceCollection()
                .AddSingleton(imageLocationService)
                .AddSingleton<ImageManagementService>();

            if (sharedCache != null)
            {
                serviceCollection.AddSingleton(sharedCache);
            }
            else
            {
                serviceCollection.AddSingleton<ISharedCache, TestCache>();
            }

            var buildServiceProvider = serviceCollection
                .BuildServiceProvider();

            return buildServiceProvider.MustGetService<ImageManagementService>();
        }
    }
}