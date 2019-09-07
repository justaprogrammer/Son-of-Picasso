using System.Collections.Generic;

namespace SonOfPicasso.Data.Model
{
    public class Directory
    {
        public int DirectoryId { get; set; }

        public string Path { get; set; }

        public List<Image> Images { get; set; }
    }
}