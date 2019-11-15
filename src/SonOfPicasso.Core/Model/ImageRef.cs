using System;
using SonOfPicasso.Data.Model;

namespace SonOfPicasso.Core.Model
{
    public class ImageRef
    {
        public ImageRef(Image image, IImageContainer imageContainer)
        {
            Id = image.Id;
            Key = $"{imageContainer.Key}:Image:{image.Id}";
            ImagePath = image.Path;
            Date = image.ExifData.DateTime;
            ContainerKey = imageContainer.Key;
            ContainerType = imageContainer.ContainerType;
            ContainerDate = imageContainer.Date;
        }

        public ImageRef(int id, string key, string imagePath, DateTime date, string containerKey,
            ImageContainerTypeEnum containerType, DateTime containerDate)
        {
            Id = id;
            Key = key;
            ImagePath = imagePath;
            Date = date;
            ContainerKey = containerKey;
            ContainerType = containerType;
            ContainerDate = containerDate;
        }

        public int Id { get; set; }
        public string Key { get; set; }
        public string ImagePath { get; set; }
        public DateTime Date { get; set; }
        public string ContainerKey { get; set; }
        public ImageContainerTypeEnum ContainerType { get; set; }
        public DateTime ContainerDate { get; set; }
    }
}