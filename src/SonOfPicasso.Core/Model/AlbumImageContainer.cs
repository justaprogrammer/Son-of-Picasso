using System;
using System.Collections.Generic;
using System.Linq;
using SonOfPicasso.Data.Model;

namespace SonOfPicasso.Core.Model
{
    public class AlbumImageContainer : ImageContainer
    {
        public AlbumImageContainer(Album album)
        {
            Id = GetContainerId(album);
            Name = album.Name;
            Date = album.Date;
            Images = album.AlbumImages.Select(albumImage => new ImageRef(albumImage.Image)).ToArray();
        }

        public override string Id { get; }
        public override string Name { get; }
        public override DateTime Date { get; }
        public override ImageContainerTypeEnum ContainerType => ImageContainerTypeEnum.Album;
        public override IList<ImageRef> Images { get; }

        public static string GetContainerId(Album album)
        {
            return $"Album{album.Id}";
        }
    }
}