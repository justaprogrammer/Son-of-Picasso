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
    public class ImageGenerationServiceTests : IntegrationTestsBase
    {
        public ImageGenerationServiceTests(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        [Fact]
        public void CanInitialize()
        {
        }
    }
}