using System.Collections.Generic;

namespace SonOfPicasso.Data.Model
{
    public class Image: IImage
    {
        public int Id { get; set; }

        public int DirectoryId { get; set; }

        public string Path { get; set; }

        public Directory Directory { get; set; }
    }

    public interface IImage: IModel
    {
    }
}