using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using SonOfPicasso.Data.Context;
using SonOfPicasso.Data.Repository;
using Xunit.Abstractions;

namespace SonOfPicasso.Testing.Common
{
    public abstract class DataTestsBase : TestsBase, IDisposable
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
}