using NSubstitute;
using SonOfPicasso.Testing.Common;
using SonOfPicasso.UI.Tests.Extensions;
using SonOfPicasso.UI.Tests.Scheduling;
using SonOfPicasso.UI.ViewModels;
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

        [Fact]
        public void Initialize()
        {
            var applicationViewModel = this.CreateApplicationViewModel();
            applicationViewModel.Initialize();
        }
    }
}
