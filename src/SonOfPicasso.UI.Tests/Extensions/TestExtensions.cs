using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using SonOfPicasso.Core.Interfaces;
using SonOfPicasso.Core.Scheduling;
using SonOfPicasso.Testing.Common;
using SonOfPicasso.Testing.Common.Extensions;
using SonOfPicasso.Testing.Common.Scheduling;
using SonOfPicasso.Testing.Common.Services;
using SonOfPicasso.UI.Interfaces;
using SonOfPicasso.UI.Scheduling;
using SonOfPicasso.UI.ViewModels;

namespace SonOfPicasso.UI.Tests.Extensions
{
    public static class TestExtensions
    {
        public static ApplicationViewModel CreateApplicationViewModel<T>(this TestsBase<T> tests,
            IFileSystem fileSystem = null,
            ISharedCache sharedCache = null,
            IImageLocationService imageLocationService = null,
            ISchedulerProvider schedulerProvider = null,
            IImageFolderViewModel imageFolderViewModel = null)
        {
            fileSystem = fileSystem ?? new MockFileSystem();
            imageLocationService = imageLocationService ?? Substitute.For<IImageLocationService>();
            schedulerProvider = schedulerProvider ?? new TestSchedulerProvider();
            imageFolderViewModel = imageFolderViewModel ?? new ImageFolderViewModel();

            var serviceCollection = tests.GetServiceCollection()
                .AddSingleton(fileSystem)
                .AddSingleton(schedulerProvider)
                .AddSingleton(imageLocationService)
                .AddSingleton(imageFolderViewModel)
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

            return buildServiceProvider.MustGetService<ApplicationViewModel>();
        }
    }
}