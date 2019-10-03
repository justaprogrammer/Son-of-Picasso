using System;
using SonOfPicasso.Data.Model;

namespace SonOfPicasso.Core.Model
{
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