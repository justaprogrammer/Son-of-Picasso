using System;
using SonOfPicasso.Data.Model;

namespace SonOfPicasso.Core.Model
{
    public class ImageRef
    {
        public ImageRef(Image image, ImageContainer imageContainer)
        {
            Id = image.Id;
            Date = image.ExifData.DateTime;
            ContainerId = imageContainer.Id;
            ContainerType = imageContainer.ContainerType;
            ContainerDate = imageContainer.Date;
        }
        public int Id { get; }
        public DateTime Date { get; }
        public string ContainerId { get; }
        public ImageContainerTypeEnum ContainerType { get; }
        public DateTime ContainerDate { get; }
    }
}