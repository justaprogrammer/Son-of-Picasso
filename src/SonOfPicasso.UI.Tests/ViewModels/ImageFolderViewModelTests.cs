using Autofac.Extras.NSubstitute;
using SonOfPicasso.Core.Scheduling;
using SonOfPicasso.Testing.Common;
using SonOfPicasso.Testing.Common.Scheduling;
using SonOfPicasso.UI.ViewModels;
using Xunit;
using Xunit.Abstractions;

namespace SonOfPicasso.UI.Tests.ViewModels
{
    public class ImageFolderViewModelTests : TestsBase
    {
        public ImageFolderViewModelTests(ITestOutputHelper testOutputHelper)
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

                var imageFolderViewModel = autoSub.Resolve<ImageFolderViewModel>();
                imageFolderViewModel.Activator.Activate();
            }
        }
    }
}