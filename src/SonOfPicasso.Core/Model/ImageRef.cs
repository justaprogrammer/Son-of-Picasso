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

        public int Id { get; }
        public string Key { get; }
        public string ImagePath { get; }
        public DateTime CreationTime { get; }
        public DateTime LastWriteTime { get; }
        public DateTime ExifDate { get; }
        public string ContainerKey { get; }
        public ImageContainerTypeEnum ContainerType { get; }
        public DateTime ContainerDate { get; }
    }
}