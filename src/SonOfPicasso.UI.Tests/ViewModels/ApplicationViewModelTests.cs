using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using FluentAssertions;
using NSubstitute;
using SonOfPicasso.Core.Interfaces;
using SonOfPicasso.Core.Models;
using SonOfPicasso.Testing.Common;
using SonOfPicasso.Testing.Common.Scheduling;
using SonOfPicasso.UI.Interfaces;
using SonOfPicasso.UI.Tests.Extensions;
using Xunit;
using Xunit.Abstractions;

namespace SonOfPicasso.UI.Tests.ViewModels
{
    public class ApplicationViewModelTests : TestsBase<ApplicationViewModelTests>
    {
        public ApplicationViewModelTests(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        [Fact(Timeout = 500)]
        public void CanInitialize()
        {
            Logger.Debug("CanInitialize");

            var testSchedulerProvider = new TestSchedulerProvider();
            var imageManagementService = Substitute.For<IImageManagementService>();

            imageManagementService.GetAllImages()
                .Returns(Observable.Empty<ImageModel>());

            imageManagementService.GetAllImageFolders()
                .Returns(Observable.Empty<ImageFolderModel>());

            var applicationViewModel = this.CreateApplicationViewModel(
                schedulerProvider: testSchedulerProvider, 
                imageManagementService: imageManagementService);

            var autoResetEvent = new AutoResetEvent(false);

            applicationViewModel.Initialize()
                .Subscribe(_ => autoResetEvent.Set());

            imageManagementService.Received().GetAllImages();
            imageManagementService.Received().GetAllImageFolders();

            testSchedulerProvider.MainThreadScheduler.AdvanceBy(1);

            autoResetEvent.WaitOne();
        }

        [Fact(Timeout = 500)]
        public void CanAddPath()
        {
            Logger.Debug("CanAddPath");

            var testSchedulerProvider = new TestSchedulerProvider();
            var imageManagementService = Substitute.For<IImageManagementService>();

            var directoryPath = Faker.System.DirectoryPath();

            var imageFiles = Faker.Make(5, () => Path.Combine(directoryPath, Faker.System.FileName("png")))
                .ToArray();

            var imageFolderModel = new ImageFolderModel {Path = directoryPath, Images = imageFiles};

            var imageModels = imageFiles
                .Select(path => new ImageModel { Path = path })
                .ToArray();

            imageManagementService.AddFolder(directoryPath)
                .Returns(_ => Observable.Return((imageFolderModel, imageModels)));

            var imageViewModels = new List<IImageViewModel>();
            var imageFolderViewModels = new List<IImageFolderViewModel>();

            var applicationViewModel = this.CreateApplicationViewModel(
                schedulerProvider: testSchedulerProvider,
                imageManagementService: imageManagementService,
                imageViewModelFactory: () =>
                {
                    var imageViewModel = Substitute.For<IImageViewModel>();
                    imageViewModels.Add(imageViewModel);
                    return imageViewModel;
                },
                imageFolderViewModelFactory: () =>
                {
                    var imageFolderViewModel = Substitute.For<IImageFolderViewModel>();
                    imageFolderViewModels.Add(imageFolderViewModel);
                    return imageFolderViewModel;
                });

            var autoResetEvent = new AutoResetEvent(false);

            applicationViewModel.AddFolder.Execute(directoryPath)
                .Subscribe(_ => autoResetEvent.Set());

            testSchedulerProvider.MainThreadScheduler.AdvanceBy(1);
            testSchedulerProvider.MainThreadScheduler.AdvanceBy(1);

            imageManagementService.Received().AddFolder(directoryPath);

            imageViewModels.Should().HaveCount(5);
            for (int i = 0; i < imageViewModels.Count; i++)
            {
                imageViewModels[i].Received().Initialize(imageModels[i]);
            }

            imageFolderViewModels.Should().HaveCount(1);
            imageFolderViewModels.First().Received().Initialize(imageFolderModel);

            autoResetEvent.WaitOne();
        }
    }
}
