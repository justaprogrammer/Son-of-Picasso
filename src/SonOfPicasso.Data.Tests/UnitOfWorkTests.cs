using System;
using System.Collections.Generic;
using System.IO;
using AutoBogus;
using Autofac.Core.Registration;
using Autofac.Extras.NSubstitute;
using Bogus;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SonOfPicasso.Data.Context;
using SonOfPicasso.Data.Model;
using SonOfPicasso.Data.Repository;
using SonOfPicasso.Testing.Common;
using Xunit;
using Xunit.Abstractions;
using Directory = SonOfPicasso.Data.Model.Directory;

namespace SonOfPicasso.Data.Tests
{
    public class DataTestsBase : TestsBase, IDisposable
    {
        private readonly string _databasePath;
        private readonly DbContextOptions<DataContext> _dbContextOptions;

        public DataTestsBase(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
            var databaseRoot = Path.Join(Path.GetTempPath(), "SonOfPicasso.Data.Tests");
            var directoryInfo = new DirectoryInfo(databaseRoot);
            if (!directoryInfo.Exists)
            {
                directoryInfo.Create();
            }

            _databasePath = Path.Join(databaseRoot, $"{Guid.NewGuid()}.db");

            _dbContextOptions =
                new DbContextOptionsBuilder<DataContext>()
                    .UseSqlite($"Data Source={_databasePath}")
                    .Options;

            using var dataContext = new DataContext(_dbContextOptions);
            dataContext.Database.EnsureCreated();
        }

        protected UnitOfWork CreateUnitOfWork()
        {
            return new UnitOfWork(_dbContextOptions);
        }

        public void Dispose()
        {
            if (File.Exists(_databasePath))
            {
                File.Delete(_databasePath);
            }
        }
    }

    public class UnitOfWorkTests : DataTestsBase
    {
        public static readonly Faker<Directory> FakeNewDirectory
            = new AutoFaker<Directory>().RuleFor(directory1 => directory1.Id, 0)
                .RuleFor(directory1 => directory1.Images, (List<Image>)null)
                .RuleFor(directory1 => directory1.Path, faker => faker.System.FilePath());

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