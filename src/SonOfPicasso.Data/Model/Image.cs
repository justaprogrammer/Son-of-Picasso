using System.Collections.Generic;

namespace SonOfPicasso.Data.Model
{
    public class Image
    {
        public int ImageId { get; set; }

        public int DirectoryId { get; set; }

        public string Path { get; set; }

        public Directory Directory { get; set; }
    }
}