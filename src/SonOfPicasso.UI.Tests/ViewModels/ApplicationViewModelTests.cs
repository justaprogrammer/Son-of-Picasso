using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Documents;
using Autofac.Extras.NSubstitute;
using FluentAssertions;
using NSubstitute;
using NSubstitute.Routing.AutoValues;
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
            AutoSubstitute.Provide<Func<ImageContainerViewModel>>(() => new ImageContainerViewModel(new ViewModelActivator()));
            AutoSubstitute.Provide<Func<ImageViewModel>>(() => new ImageViewModel(AutoSubstitute.Resolve<IImageLoadingService>(), TestSchedulerProvider, new ViewModelActivator()));

            var imageManagementService = AutoSubstitute.Resolve<IImageManagementService>();

            ImageContainer[] imageContainers = {
                new FolderImageContainer(Fakers.FolderFaker), 
                new AlbumImageContainer(Fakers.AlbumFaker), 
            };

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
