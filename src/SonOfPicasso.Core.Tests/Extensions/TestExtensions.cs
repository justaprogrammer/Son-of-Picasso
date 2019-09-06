using System;
using System.IO.Abstractions;
using Akavache;
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
        public static ImageLocationService CreateImageLocationService<T>(this TestsBase<T> tests,
            IFileSystem fileSystem = null,
            ISchedulerProvider schedulerProvider = null)
        {
            throw new NotImplementedException();

//            fileSystem = fileSystem ?? new MockFileSystem();
//            schedulerProvider = schedulerProvider ?? new TestSchedulerProvider();
//
//            var serviceCollection = tests.GetServiceCollection()
//                .AddSingleton(fileSystem)
//                .AddSingleton(schedulerProvider)
//                .AddSingleton<ImageLocationService>();
//
//            var buildServiceProvider = serviceCollection
//                .BuildServiceProvider();
//
//            return buildServiceProvider.MustGetService<ImageLocationService>();
        }

        public static ImageManagementService CreateImageManagementService<T>(this TestsBase<T> tests,
            IDataCache dataCache = null,
            IImageLocationService imageLocationService = null)
        {
            throw new NotImplementedException();

//            imageLocationService = imageLocationService ?? Substitute.For<IImageLocationService>();
//
//            var serviceCollection = tests.GetServiceCollection()
//                .AddSingleton(imageLocationService)
//                .AddSingleton<ImageManagementService>();
//
//            if (dataCache != null)
//            {
//                serviceCollection.AddSingleton(dataCache);
//            }
//            else
//            {
//                serviceCollection.AddSingleton<IDataCache, TestCache>();
//            }
//
//            var buildServiceProvider = serviceCollection
//                .BuildServiceProvider();
//
//            return buildServiceProvider.MustGetService<ImageManagementService>();
        }
    }
}