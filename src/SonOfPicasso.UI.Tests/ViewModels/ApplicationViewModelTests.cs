using System;
using System.Linq;
using System.Reactive.Linq;
using FluentAssertions;
using NSubstitute;
using ReactiveUI;
using SonOfPicasso.Core.Interfaces;
using SonOfPicasso.Core.Model;
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
            AutoSubstitute.Provide<Func<ImageContainerViewModel>>(() =>
                new ImageContainerViewModel(new ViewModelActivator()));
            AutoSubstitute.Provide<Func<ImageViewModel>>(() =>
                new ImageViewModel(AutoSubstitute.Resolve<IImageLoadingService>(), TestSchedulerProvider,
                    new ViewModelActivator()));

            var imageManagementService = AutoSubstitute.Resolve<IImageManagementService>();

            var startDate = Faker.Date.Past().Date;

            var dictionary =
                Faker.MakeLazy(2, i => startDate.AddDays(i))
                    .ToDictionary(time => time, time => Faker
                        .MakeLazy(4, () => Faker.Date.Between(time, time.AddDays(1).AddSeconds(-1)))
                        .ToArray());

            var imageContainers = dictionary.Select((pair, i) =>
            {
                var folder = Fakers.FolderFaker.Generate();
                folder.Id = i;
                folder.Date = pair.Key;
                folder.Images = pair.Value.Select(time =>
                {
                    var image = Fakers.ImageFaker.Generate();
                    image.Path = MockFileSystem.Path.Combine(folder.Path, time.ToLongTimeString() + ".png");
                    image.ExifData.DateTime = time;
                    image.ExifData.DateTimeDigitized = time;
                    image.ExifData.DateTimeOriginal = time;
                    image.ExifData.DateTimeDigitized = time;
                    return image;
                }).ToList();

                return (ImageContainer) new FolderImageContainer(folder);
            }).ToArray();

            imageManagementService.GetAllImageContainers()
                .Returns(imageContainers
                    .ToObservable()
                    .SubscribeOn(TestSchedulerProvider.TaskPool));

            var applicationViewModel = AutoSubstitute.Resolve<ApplicationViewModel>();
            applicationViewModel.Activator.Activate();

            imageManagementService.Received(1)
                .GetAllImageContainers();

            TestSchedulerProvider.TaskPool.AdvanceBy(1);
            TestSchedulerProvider.MainThreadScheduler.AdvanceBy(2);

            applicationViewModel.ImageContainers.Count.Should().Be(2);
        }
    }
}