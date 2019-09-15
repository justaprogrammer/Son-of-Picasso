using System;
using System.IO.Abstractions;
using Microsoft.EntityFrameworkCore;
using SonOfPicasso.Data.Context;
using SonOfPicasso.Data.Repository;
using SonOfPicasso.Testing.Common;
using Xunit.Abstractions;

namespace SonOfPicasso.Data.Tests
{
    public abstract class DataTestsBase : TestsBase
    {
        protected readonly DbContextOptions<DataContext> DbContextOptions;
        protected readonly FileSystem FileSystem;
        protected readonly string TestRoot;

        public DataTestsBase(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
            FileSystem = new FileSystem();

            TestRoot = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), "SonOfPicasso.Data.Tests",
                Guid.NewGuid().ToString());
            FileSystem.Directory.CreateDirectory(TestRoot);

            DbContextOptions =
                new DbContextOptionsBuilder<DataContext>()
                    .UseInMemoryDatabase($"{Guid.NewGuid().ToString()}")
                    .Options;

            using var dataContext = new DataContext(DbContextOptions);
            dataContext.Database.EnsureCreated();
        }

        public override void Dispose()
        {
            base.Dispose();

            if (FileSystem.File.Exists(TestRoot))
                try
                {
                    FileSystem.File.Delete(TestRoot);
                }
                catch (Exception e)
                {
                    Logger.Error(e, "Unable to delete test directory {TestRoot}", TestRoot);
                }
        }

        protected UnitOfWork CreateUnitOfWork()
        {
            return new UnitOfWork(DbContextOptions);
        }
    }
}