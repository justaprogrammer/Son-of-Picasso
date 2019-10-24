using System;
using System.IO.Abstractions;
using Microsoft.EntityFrameworkCore;
using SonOfPicasso.Data.Repository;
using SonOfPicasso.Testing.Common;
using Xunit.Abstractions;

namespace SonOfPicasso.Integration.Tests
{
    public abstract class IntegrationTestsBase : TestsBase, IDisposable
    {
        protected readonly string DatabasePath;
        protected readonly DbContextOptions<DataContext> DbContextOptions;
        protected readonly FileSystem FileSystem;
        protected readonly string TestPath;

        public IntegrationTestsBase(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
            FileSystem = new FileSystem();

            TestPath = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), "SonOfPicasso.IntegrationTests",
                Guid.NewGuid().ToString());
            FileSystem.Directory.CreateDirectory(TestPath);

            DatabasePath = FileSystem.Path.Combine(TestPath, "database.db");

            DbContextOptions =
                new DbContextOptionsBuilder<DataContext>()
                    .UseSqlite($"Data Source={DatabasePath}")
                    .Options;

            using var dataContext = new DataContext(DbContextOptions);
            dataContext.Database.Migrate();
        }

        public override void Dispose()
        {
            base.Dispose();

            if (FileSystem.Directory.Exists(TestPath))
                try
                {
                    FileSystem.Directory.Delete(TestPath, true);
                }
                catch (Exception e)
                {
                    Logger.Error(e, "Unable to delete test directory {TestPath}", TestPath);
                }
        }
    }
}