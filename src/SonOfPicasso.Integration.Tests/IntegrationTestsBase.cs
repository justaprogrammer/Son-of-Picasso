using System;
using System.Data.Common;
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
        protected readonly DbConnection Connection;
        private readonly DataContext _dataContext;

        protected IntegrationTestsBase(ITestOutputHelper testOutputHelper)
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

            _dataContext = new DataContext(DbContextOptions);
            _dataContext.Database.Migrate();
            _dataContext.Database.OpenConnection();
            Connection = _dataContext.Database.GetDbConnection(); 
        }

        public override void Dispose()
        {
            base.Dispose();
            Connection?.Dispose();
            _dataContext?.Dispose();

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