using System;
using SonOfPicasso.Data.Model;

namespace SonOfPicasso.Core.Model
{
    public class ImageRef
    {
        public ImageRef(Image image, IImageContainer imageContainer)
        {
            Id = $"{imageContainer.Id}:Image:{image.Id}";
            ImageId = image.Id;
            ImagePath = image.Path;
            Date = image.ExifData.DateTime;
            ContainerId = imageContainer.Id;
            ContainerType = imageContainer.ContainerType;
            ContainerDate = imageContainer.Date;
        }

        public ImageRef(string id, string imagePath, int imageId, DateTime date, string containerId,
            ImageContainerTypeEnum containerType, DateTime containerDate)
        {
            Id = id;
            ImagePath = imagePath;
            ImageId = imageId;
            Date = date;
            ContainerId = containerId;
            ContainerType = containerType;
            ContainerDate = containerDate;
        }

        public string Id { get; }
        public string ImagePath { get; }
        public int ImageId { get; }
        public DateTime Date { get; }
        public string ContainerId { get; }
        public ImageContainerTypeEnum ContainerType { get; }
        public DateTime ContainerDate { get; }
    }
}