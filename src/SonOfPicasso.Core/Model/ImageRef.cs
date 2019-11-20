using System;
using SonOfPicasso.Data.Model;

namespace SonOfPicasso.Core.Model
{
    public class ImageRef
    {
        public ImageRef(Image image, IImageContainer imageContainer)
            : this(image.Id, $"{imageContainer.Key}:Image:{image.Id}", image.Path, image.CreationTime,
                image.LastWriteTime, image.ExifData.DateTime, imageContainer.Key, imageContainer.ContainerType,
                imageContainer.Date)
        {
        }

        public ImageRef(int id, string key, string imagePath, DateTime creationTime, DateTime lastWriteTime,
            DateTime exifDate, string containerKey, ImageContainerTypeEnum containerType, DateTime containerDate)
        {
            Id = id;
            Key = key;
            ImagePath = imagePath;
            CreationTime = creationTime;
            LastWriteTime = lastWriteTime;
            ExifDate = exifDate;
            ContainerKey = containerKey;
            ContainerType = containerType;
            ContainerDate = containerDate;
        }

        public ImageRef()
        {
        }

        public int Id { get; set; }
        public string Key { get; set;}
        public string ImagePath { get; set;}
        public DateTime CreationTime { get;set; }
        public DateTime LastWriteTime { get; set;}
        public DateTime ExifDate { get; set;}
        public string ContainerKey { get; set;}
        public ImageContainerTypeEnum ContainerType { get; set;}
        public DateTime ContainerDate { get; set;}
    }
}