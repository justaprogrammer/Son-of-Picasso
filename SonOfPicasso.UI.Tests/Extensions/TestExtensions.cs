using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using SonOfPicasso.Core.Interfaces;
using SonOfPicasso.Testing.Common;
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
            sharedCache = sharedCache ?? Substitute.For<ISharedCache>();
            imageLocationService = imageLocationService ?? Substitute.For<IImageLocationService>();
            schedulerProvider = schedulerProvider ?? new TestSchedulerProvider();

            var serviceCollection = databaseReaderTests.GetServiceCollection()
                .AddSingleton(fileSystem)
                .AddSingleton(schedulerProvider)
                .AddSingleton(imageLocationService)
                .AddSingleton(sharedCache)
                .AddSingleton(typeof(ApplicationViewModel));

            var buildServiceProvider = serviceCollection
                .BuildServiceProvider();

            return buildServiceProvider.GetService<ApplicationViewModel>();
        }
    }
}