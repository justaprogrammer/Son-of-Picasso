﻿using System.IO.Abstractions;
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
            IDataCache dataCache = null,
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

            if (dataCache != null)
            {
                serviceCollection.AddSingleton(dataCache);
            }
            else
            {
                serviceCollection.AddSingleton<IDataCache, TestCache>();
            }

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