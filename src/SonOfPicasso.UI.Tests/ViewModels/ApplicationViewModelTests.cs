using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Documents;
using FluentAssertions;
using NSubstitute;
using ReactiveUI;
using SonOfPicasso.Core.Interfaces;
using SonOfPicasso.Data.Model;
using SonOfPicasso.Testing.Common;
using SonOfPicasso.UI.ViewModels;
using Xunit;
using Xunit.Abstractions;

namespace SonOfPicasso.UI.Tests.ViewModels
{
    public class ApplicationViewModelTests : UnitTestsBase
    {
        public ApplicationViewModelTests(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        [Fact]
        public void CanActivate()
        {
            AutoSubstitute.Provide<Func<ImageFolderViewModel>>(() => new ImageFolderViewModel(new ViewModelActivator()));
            AutoSubstitute.Provide<Func<ImageViewModel>>(() => new ImageViewModel(AutoSubstitute.Resolve<IImageLoadingService>(), TestSchedulerProvider, new ViewModelActivator()));

            var imageManagementService = AutoSubstitute.Resolve<IImageManagementService>();

            var startDate = Faker.Date.Past(1).Date;

            var folderDates = Enumerable.Range(0, 2)
                .Select(i => startDate.AddDays(i))
                .ToArray();

            var images = folderDates
                .Select((folderDate, i) =>
                {
                    var folder = Fakers.FolderFaker.Generate();
                    folder.Id = i;

                    folder.Images.AddRange(Fakers.ImageFaker.GenerateLazy(4)
                        .Select(image =>
                        {
                            var imageDate = Faker.Date.Between(folderDate, folderDate.AddDays(1).AddSeconds(-1));
                            image.Path = MockFileSystem.Path.Combine(folder.Path, Faker.System.FileName("jpg"));
                            image.ExifData.DateTime = imageDate;
                            image.ExifData.DateTimeDigitized = imageDate;
                            image.ExifData.DateTimeOriginal = imageDate;
                            image.ExifData.ThumbnailDateTime = imageDate;
                            image.Folder = folder;
                            image.FolderId = folder.Id;

                            return image;
                        }));
                    folder.Date = folderDate;

                    return folder;
                })
                .SelectMany(folder => folder.Images)
                .ToArray();

            imageManagementService.GetImagesWithDirectoryAndExif()
                .Returns(images.ToObservable().SubscribeOn(TestSchedulerProvider.TaskPool));

            imageManagementService.GetAllAlbumsWithAlbumImages()
                .Returns(Observable.Empty<Album>().SubscribeOn(TestSchedulerProvider.TaskPool));

            var applicationViewModel = AutoSubstitute.Resolve<ApplicationViewModel>();
            applicationViewModel.Activator.Activate();

            imageManagementService.Received(1)
                .GetImagesWithDirectoryAndExif();

            imageManagementService.Received(1)
                .GetAllAlbumsWithAlbumImages();
            
            TestSchedulerProvider.TaskPool.AdvanceBy(2);
            TestSchedulerProvider.MainThreadScheduler.AdvanceBy(8);

            // applicationViewModel.Images.Count.Should().Be(8);
            applicationViewModel.ImageContainers.Count.Should().Be(2);

            applicationViewModel.ImageContainers.Select((model, i) => model.Date)
                .Should().BeEquivalentTo(folderDates);

            // applicationViewModel.Images.Select((model, i) => model.ExifData)
            //    .Should().BeEquivalentTo(folderDates);
        }
    }
}
