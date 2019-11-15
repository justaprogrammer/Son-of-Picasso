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
            Key = GetContainerKey(album);
            Id = album.Id;
            Name = album.Name;
            Date = album.Date;
            Year = album.Date.Year;
            ImageRefs = album.AlbumImages.Select(albumImage => new ImageRef(albumImage.Image, this)).ToArray();
        }

        public int Id { get; }
        public string Key { get; }
        public string Name { get; }
        public int Year { get; }
        public DateTime Date { get; }
        public ImageContainerTypeEnum ContainerType => ImageContainerTypeEnum.Album;
        public IList<ImageRef> ImageRefs { get; }

        public static string GetContainerKey(Album album)
        {
            return GetContainerKey(album.Id);
        }
        
        public static string GetContainerKey(int albumId)
        {
            return $"Album:{albumId}";
        }
    }
}