using Autofac.Extras.NSubstitute;
using FluentAssertions;
using NSubstitute;
using SonOfPicasso.Core.Interfaces;
using SonOfPicasso.Core.Scheduling;
using SonOfPicasso.Testing.Common;
using SonOfPicasso.Testing.Common.Scheduling;
using SonOfPicasso.UI.ViewModels;
using Xunit;
using Xunit.Abstractions;

namespace SonOfPicasso.UI.Tests.ViewModels
{
    public class ApplicationViewModelTests : TestsBase
    {
        public ApplicationViewModelTests(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }
        
        [Fact]
        public void CanActivate()
        {
            using (var autoSub = new AutoSubstitute())
            {
                var testSchedulerProvider = new TestSchedulerProvider();
                autoSub.Provide<ISchedulerProvider>(testSchedulerProvider);

                var imageManagementService = autoSub.Resolve<IImageManagementService>();

                var applicationViewModel = autoSub.Resolve<ApplicationViewModel>();
                applicationViewModel.Activator.Activate();

                imageManagementService.Received(1)
                    .GetImagesWithDirectoryAndExif();

                imageManagementService.Received(1)
                    .GetAllAlbumsWithAlbumImages();
            }
        }
    }
}
