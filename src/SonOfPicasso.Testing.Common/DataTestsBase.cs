using System;
using System.IO.Abstractions;
using System.IO.Abstractions;
using Microsoft.EntityFrameworkCore;
using SonOfPicasso.Data.Context;
using SonOfPicasso.Data.Repository;
using Xunit.Abstractions;

namespace SonOfPicasso.Testing.Common
{
    public abstract class DataTestsBase : TestsBase, IDisposable
    {
        protected readonly string DatabasePath;
        protected readonly DbContextOptions<DataContext> DbContextOptions;
        protected readonly string TestRoot;
        protected readonly FileSystem FileSystem;

        public DataTestsBase(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
            FileSystem = new FileSystem();

            TestRoot = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), "SonOfPicasso.Data.Tests", Guid.NewGuid().ToString());
            FileSystem.Directory.CreateDirectory(TestRoot);

            DatabasePath = FileSystem.Path.Combine(TestRoot, $"database.db");

            DbContextOptions =
                new DbContextOptionsBuilder<DataContext>()
                    .UseSqlite($"Data Source={DatabasePath}")
                    .Options;

            using var dataContext = new DataContext(DbContextOptions);
            dataContext.Database.EnsureCreated();
        }

        protected UnitOfWork CreateUnitOfWork()
        {
            return new UnitOfWork(DbContextOptions);
        }

        public void Dispose()
        {
            if (FileSystem.File.Exists(TestRoot))
            {
                try
                {
                    FileSystem.File.Delete(TestRoot);
                }
                catch (Exception e)
                {
                    Logger.Error(e, "Unable to delete test directory {TestRoot}", TestRoot);
                }
            }
        }
    }
}