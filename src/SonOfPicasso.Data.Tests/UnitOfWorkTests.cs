using System;
using System.Collections.Generic;
using AutoBogus;
using Autofac.Extras.NSubstitute;
using Bogus;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SonOfPicasso.Data.Model;
using SonOfPicasso.Testing.Common;
using Xunit;
using Xunit.Abstractions;

namespace SonOfPicasso.Data.Tests
{
    public class UnitOfWorkTests : TestsBase
    {
        private readonly Faker<Directory> _newDirectoryFaker = new AutoFaker<Directory>()
            .RuleFor(directory1 => directory1.DirectoryId, 0)
            .RuleFor(directory1 => directory1.Images, (List<Image>)null)
            .RuleFor(directory1 => directory1.Path, faker => faker.System.FilePath());

        public UnitOfWorkTests(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        [Fact]
        public void CanSaveAndGetById()
        {
            using (var autoSub = new AutoSubstitute())
            {
                var dbContextOptions =
                    new DbContextOptionsBuilder<DataContext>()
                    .UseInMemoryDatabase("UnitOfWorkTests");

                using (var dataContext = new DataContext(dbContextOptions.Options))
                {
                    autoSub.Provide<IDataContext>(dataContext);
                    var unitOfWorkFactory = autoSub.Resolve<Func<UnitOfWork>>();

                    var directory = (Directory)_newDirectoryFaker;

                    using (var unitOfWork = unitOfWorkFactory())
                    {
                        unitOfWork.DirectoryRepository.Insert(directory);
                        unitOfWork.Save();
                    }

                    using (var unitOfWork = unitOfWorkFactory())
                    {
                        var dircopy = unitOfWork.DirectoryRepository.GetById(directory.DirectoryId);
                        dircopy.Path.Should().Be(dircopy.Path);
                    }
                }
            }
        }
    }
}