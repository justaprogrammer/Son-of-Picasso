using System;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using Microsoft.EntityFrameworkCore;
using SonOfPicasso.Data.Repository;
using SonOfPicasso.Data.Services;
using SonOfPicasso.Testing.Common;
using Xunit.Abstractions;

namespace SonOfPicasso.Data.Tests
{
    public abstract class DataTestsBase : UnitTestsBase
    {
        protected readonly DbContextOptions<DataContext> DbContextOptions;
        protected readonly FileSystem FileSystem;

        public DataTestsBase(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
            DbContextOptions =
                new DbContextOptionsBuilder<DataContext>()
                    .UseInMemoryDatabase($"{Guid.NewGuid().ToString()}")
                    .Options;

            using var dataContext = new DataContext(DbContextOptions);
            dataContext.Database.EnsureCreated();
        }

        protected UnitOfWork CreateUnitOfWork()
        {
            return new UnitOfWork(DbContextOptions);
        }
    }
}