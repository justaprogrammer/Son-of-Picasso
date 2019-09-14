using System;
using System.Reactive.Linq;
using Autofac.Extras.NSubstitute;
using SonOfPicasso.Data.Repository;
using SonOfPicasso.Testing.Common;
using SonOfPicasso.Testing.Common.Services;
using Xunit;
using Xunit.Abstractions;

namespace SonOfPicasso.Core.IntegrationTests.Services
{
    public class ImageManagementServiceTests : TestsBase
    {
        private readonly AutoSubstitute _autoSubstitute;
        private readonly string _imagesPath;

        public ImageManagementServiceTests(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        [Fact]
        public void CanInitialize()
        {
            var imageManagementService = _autoSubstitute.Resolve<Core.Services.ImageManagementService>();
        }
    }
}