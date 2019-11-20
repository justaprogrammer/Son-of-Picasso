using System;
using SonOfPicasso.Data.Model;

namespace SonOfPicasso.Core.Model
{
    public class ImageRef
    {
        private string _key;

        public ImageRef(int id, string imagePath, DateTime creationTime, DateTime lastWriteTime,
            DateTime exifDate, string containerKey, ImageContainerTypeEnum containerType, DateTime containerDate)
        {
            Id = id;
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
        public string Key => _key ??= $"{ContainerKey}:Image:{Id}";
        public string ImagePath { get; set; }
        public DateTime CreationTime { get; set; }
        public DateTime LastWriteTime { get; set; }
        public DateTime ExifDate { get; set; }
        public string ContainerKey { get; set; }
        public ImageContainerTypeEnum ContainerType { get; set; }
        public DateTime ContainerDate { get; set; }

        public static ImageRef CreateImageRef(Image image, IImageContainer imageContainer)
        {
            return new ImageRef(image.Id, image.Path, image.CreationTime,
                image.LastWriteTime, image.ExifData.DateTime, imageContainer.Key, imageContainer.ContainerType,
                imageContainer.Date);
        }
    }
}