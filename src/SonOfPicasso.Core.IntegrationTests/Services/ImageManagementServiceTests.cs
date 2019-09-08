using System;
using Autofac.Extras.NSubstitute;
using SonOfPicasso.Data.Repository;
using SonOfPicasso.Data.Tests;
using Xunit;
using Xunit.Abstractions;

namespace SonOfPicasso.Core.Tests.IntegrationTests.Services
{
    public class ImageManagementServiceTests : DataTestsBase, IDisposable
    {
        private readonly AutoSubstitute _autoSubstitute;

        public ImageManagementServiceTests(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
            _autoSubstitute = new AutoSubstitute();
            _autoSubstitute.Provide<Func<UnitOfWork>>(CreateUnitOfWork);
        }

        [Fact]
        public void Blah()
        {
            var imageManagementService = _autoSubstitute.Resolve<Core.Services.ImageManagementService>();
        }

        public new void Dispose()
        {
            _autoSubstitute.Dispose();
            base.Dispose();
        }
    }
}