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
            ContainerTypeId = album.Id;
            Name = album.Name;
            Date = album.Date;
            Year = album.Date.Year;
            ImageRefs = album.AlbumImages.Select(albumImage => new ImageRef(albumImage.Image, this)).ToArray();
        }

        public override string Id { get; }
        public override string Name { get; }
        public override int Year { get; }
        public override DateTime Date { get; }
        public override ImageContainerTypeEnum ContainerType => ImageContainerTypeEnum.Album;
        public override int ContainerTypeId { get; }
        public override IList<ImageRef> ImageRefs { get; }

        public static string GetContainerId(Album album)
        {
            return $"Album:{album.Id}";
        }
    }
}