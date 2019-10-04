using System;
using SonOfPicasso.Data.Model;

namespace SonOfPicasso.Core.Model
{
    public class ImageRef
    {
        public ImageRef(Image image)
        {
            Id = image.Id;
            Date = image.ExifData.DateTime;
        }

        public DateTime Date { get; }

        public int Id { get; }
    }
}