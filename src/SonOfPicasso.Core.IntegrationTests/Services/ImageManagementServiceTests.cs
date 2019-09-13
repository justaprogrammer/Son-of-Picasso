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
    public class ImageManagementServiceTests : DataTestsBase, IDisposable
    {
        private readonly AutoSubstitute _autoSubstitute;
        private readonly string _imagesPath;

        public ImageManagementServiceTests(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
            _autoSubstitute = new AutoSubstitute();
            _autoSubstitute.Provide<Func<UnitOfWork>>(CreateUnitOfWork);

            _imagesPath = FileSystem.Path.Combine(TestRoot, "Images");
            FileSystem.Directory.CreateDirectory(_imagesPath);

            var imageGenerationService = new ImageGenerationService(Logger.ForContext<ImageGenerationService>(), FileSystem);
            imageGenerationService.GenerateImages(10, _imagesPath).Wait();
        }

        [Fact]
        public void CanInitialize()
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