using System.Collections.Generic;
using AutoBogus;
using Bogus;
using FluentAssertions;
using SonOfPicasso.Data.Model;
using SonOfPicasso.Testing.Common;
using SonOfPicasso.Testing.Common.Extensions;
using Xunit;
using Xunit.Abstractions;

namespace SonOfPicasso.Data.Tests.Repository
{
    public class UnitOfWorkTests : DataTestsBase
    {
        private static readonly Faker<Directory> FakeNewDirectory
            = new AutoFaker<Directory>().RuleFor(directory1 => directory1.Id, 0)
                .RuleFor(directory1 => directory1.Images, (List<Image>)null)
                .RuleFor(directory1 => directory1.Path, faker => faker.System.DirectoryPathWindows());

        public UnitOfWorkTests(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        [Fact]
        public void CanSaveAndGetById()
        {
            var directory = FakeNewDirectory.Generate();
            
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