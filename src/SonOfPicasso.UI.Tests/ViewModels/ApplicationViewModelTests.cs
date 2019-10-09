using System.Linq;
using System.Reactive.Linq;
using FluentAssertions;
using MoreLinq;
using NSubstitute;
using SonOfPicasso.Core.Interfaces;
using SonOfPicasso.Core.Model;
using SonOfPicasso.Testing.Common;
using SonOfPicasso.UI.ViewModels;
using Xunit;
using Xunit.Abstractions;

namespace SonOfPicasso.UI.Tests.ViewModels
{
    public class ApplicationViewModelTests : ViewModelTestsBase
    {
        public ApplicationViewModelTests(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        [Fact]
        public void ShouldInitializeAndActivate()
        {
            var imageManagementService = AutoSubstitute.Resolve<IImageManagementService>();

            var folders = Fakers.FolderFaker
                .GenerateForever("default,withImages")
                .DistinctBy(folder => folder.Date)
                .Take(2)
                .ToArray();

            var imageContainers = folders
                .Select(folder => new FolderImageContainer(folder))
                .ToArray();

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

            applicationViewModel.ImageContainerViewModels.Count.Should().Be(2);
            ActivateContainerViewModel(2, applicationViewModel.ImageContainerViewModels.ToArray());
        }
    }
}