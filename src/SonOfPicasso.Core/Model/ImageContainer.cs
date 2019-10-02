using System;
using SonOfPicasso.Data.Model;

namespace SonOfPicasso.Core.Model
{
    public enum ImageContainerTypeEnum
    {
        Album,
        Folder
    }

    public abstract class ImageContainer
    {
        public abstract string Id { get; }
        public abstract string Name { get; }
        public abstract DateTime Date { get; }
        public abstract ImageContainerTypeEnum ContainerType { get; }
    }

    public class FolderImageContainer : ImageContainer
    {
        private readonly Folder _folder;

        public FolderImageContainer(Folder folder)
        {
            _folder = folder;
        }

        public override string Id => GetContainerId(_folder);
        public override string Name => _folder.Path;
        public override DateTime Date => _folder.Date;
        public override ImageContainerTypeEnum ContainerType => ImageContainerTypeEnum.Folder;

        public static string GetContainerId(Folder folder)
        {
            return $"Folder{folder.Id}";
        }
    }

    public class AlbumImageContainer : ImageContainer
    {
        private readonly Album _album;

        public AlbumImageContainer(Album album)
        {
            _album = album;
        }

        public override string Id => GetContainerId(_album);
        public override string Name => _album.Name;
        public override DateTime Date => _album.Date;
        public override ImageContainerTypeEnum ContainerType => ImageContainerTypeEnum.Album;

        public static string GetContainerId(Album album)
        {
            return $"Album{album.Id}";
        }
    }
}