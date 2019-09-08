using FluentAssertions;
using SonOfPicasso.Testing.Common;
using Xunit;
using Xunit.Abstractions;

namespace SonOfPicasso.Data.Tests
{
    public class UnitOfWorkTests : DataTestsBase
    {
        public UnitOfWorkTests(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        [Fact]
        public void CanSaveAndGetById()
        {
            var directory = FakerProfiles.FakeNewDirectory.Generate();
            
            using (var unitOfWork = CreateUnitOfWork())
            {
                unitOfWork.DirectoryRepository.Insert(directory);
                unitOfWork.Save();
            }

            using (var unitOfWork = CreateUnitOfWork())
            {
                var dircopy = unitOfWork.DirectoryRepository.GetById(directory.Id);
                directory.Should().BeEquivalentTo(dircopy);
            }
        }
    }
}