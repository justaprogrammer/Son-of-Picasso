using System;
using System.Collections.Generic;
using System.Linq;
using SonOfPicasso.Data.Model;

namespace SonOfPicasso.Core.Model
{
    public class AlbumImageContainer : IImageContainer
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

        public string Id { get; }
        public string Name { get; }
        public int Year { get; }
        public DateTime Date { get; }
        public ImageContainerTypeEnum ContainerType => ImageContainerTypeEnum.Album;
        public int ContainerTypeId { get; }
        public IList<ImageRef> ImageRefs { get; }

        public static string GetContainerId(Album album)
        {
            return GetContainerId(album.Id);
        }
        
        public static string GetContainerId(int albumId)
        {
            return $"Album:{albumId}";
        }
    }
}