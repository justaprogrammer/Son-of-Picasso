using System;
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
using SonOfPicasso.UI.Services;
using SonOfPicasso.UI.ViewModels;

namespace SonOfPicasso.UI.Tests.Extensions
{
    public static class TestExtensions
    {
        public static ApplicationViewModel CreateApplicationViewModel<T>(this TestsBase<T> tests,
            IFileSystem fileSystem = null,
            IImageManagementService imageManagementService = null,
            ISchedulerProvider schedulerProvider = null,
            IImageFolderViewModel imageFolderViewModel = null,
            Func<IImageViewModel> imageViewModelFactory = null,
            Func<IImageFolderViewModel> imageFolderViewModelFactory = null)
        {
            TService UseFactoryIfExists<TService>(Func<TService> factory) where TService : class
            {
                return factory != null
                    ? factory() ?? throw new InvalidOperationException()
                    : Substitute.For<TService>();
            }

            fileSystem = fileSystem ?? new MockFileSystem();
            imageManagementService = imageManagementService ?? Substitute.For<IImageManagementService>();
            schedulerProvider = schedulerProvider ?? new TestSchedulerProvider();
            imageFolderViewModel = imageFolderViewModel ?? new ImageFolderViewModel();

            var serviceCollection = tests.GetServiceCollection()
                .AddSingleton(fileSystem)
                .AddSingleton(schedulerProvider)
                .AddSingleton(imageManagementService)
                .AddSingleton(imageFolderViewModel)
                .AddSingleton(typeof(ApplicationViewModel))
                .AddTransient(_ => UseFactoryIfExists(imageViewModelFactory))
                .AddTransient(_ => UseFactoryIfExists(imageFolderViewModelFactory));

            var buildServiceProvider = serviceCollection
                .BuildServiceProvider();

            return buildServiceProvider.MustGetService<ApplicationViewModel>();
        }

        public static ImageLoadingService CreateImageLoadingService<T>(this TestsBase<T> tests,
            IFileSystem fileSystem = null,
            ISchedulerProvider schedulerProvider = null)
        {
            fileSystem = fileSystem ?? new MockFileSystem();
            schedulerProvider = schedulerProvider ?? new TestSchedulerProvider();

            var serviceCollection = tests.GetServiceCollection()
                .AddSingleton(fileSystem)
                .AddSingleton(schedulerProvider)
                .AddSingleton<ImageLoadingService>();

            var buildServiceProvider = serviceCollection
                .BuildServiceProvider();

            return buildServiceProvider.MustGetService<ImageLoadingService>();
        }
    }
}