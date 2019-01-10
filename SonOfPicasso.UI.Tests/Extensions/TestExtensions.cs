using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using SonOfPicasso.Core.Interfaces;
using SonOfPicasso.Testing.Common;
using SonOfPicasso.Testing.Common.Services;
using SonOfPicasso.UI.Scheduling;
using SonOfPicasso.UI.Tests.Scheduling;
using SonOfPicasso.UI.ViewModels;

namespace SonOfPicasso.UI.Tests.Extensions
{
    public static class TestExtensions
    {
        public static ApplicationViewModel CreateApplicationViewModel<T>(this TestsBase<T> databaseReaderTests,
            IFileSystem fileSystem = null,
            ISharedCache sharedCache = null,
            IImageLocationService imageLocationService = null,
            ISchedulerProvider schedulerProvider = null)
        {
            fileSystem = fileSystem ?? new MockFileSystem();
            imageLocationService = imageLocationService ?? Substitute.For<IImageLocationService>();
            schedulerProvider = schedulerProvider ?? new TestSchedulerProvider();

            var serviceCollection = databaseReaderTests.GetServiceCollection()
                .AddSingleton(fileSystem)
                .AddSingleton(schedulerProvider)
                .AddSingleton(imageLocationService)
                .AddSingleton(typeof(ApplicationViewModel));

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

            return buildServiceProvider.GetService<ApplicationViewModel>();
        }
    }
}