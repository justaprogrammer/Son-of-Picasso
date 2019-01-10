using FluentAssertions;
using NSubstitute;
using SonOfPicasso.Core.Interfaces;
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
        public void CanInitialize()
        {
            var imageLocationService = Substitute.For<IImageLocationService>();

            var applicationViewModel = this.CreateApplicationViewModel(
                imageLocationService: imageLocationService);

            applicationViewModel.Initialize();
        }

        [Fact]
        public void ShouldHandlePathToImages()
        {
            var imageLocationService = Substitute.For<IImageLocationService>();

            var applicationViewModel = this.CreateApplicationViewModel(
                imageLocationService: imageLocationService);

            applicationViewModel.PathToImages = Faker.System.DirectoryPath();

            imageLocationService.Received().GetImages(Arg.Any<string>());
        }
    }
}
