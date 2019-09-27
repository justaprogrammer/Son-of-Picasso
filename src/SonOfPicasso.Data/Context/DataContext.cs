using Microsoft.EntityFrameworkCore;
using SonOfPicasso.Data.Model;

namespace SonOfPicasso.Data.Context
{
    public class DataContext : DbContext, IDataContext
    {
        public DbSet<Directory> Directories { get; set; }

        public DbSet<Image> Images { get; set; }

        public DbSet<Album> Albums { get; set; }

        public DbSet<AlbumImage> AlbumImages { get; set; }

        public DataContext()
        { }

        public DataContext(DbContextOptions<DataContext> options)
            : base(options)
        { }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // optionsBuilder.UseLoggerFactory(new SerilogLoggerFactory());
            // optionsBuilder.EnableSensitiveDataLogging();

            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlite("Data Source=sonofpicasso.db");
            }
        }
    }
}