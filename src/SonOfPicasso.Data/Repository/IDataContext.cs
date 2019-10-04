using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using SonOfPicasso.Data.Model;

namespace SonOfPicasso.Data.Repository
{
    public interface IDataContext
    {
        int SaveChanges();
        DbSet<TEntity> Set<TEntity>() where TEntity : class;
        EntityEntry<TEntity> Entry<TEntity>(TEntity entity) where TEntity : class;
        DbSet<Folder> Folders { get; set; }
        DbSet<Image> Images { get; set; }
        DbSet<Album> Albums { get; set; }
        DbSet<AlbumImage> AlbumImages { get; set; }
    }
}