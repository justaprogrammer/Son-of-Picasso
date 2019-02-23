using System.Collections.Generic;

namespace SonOfPicasso.Core.Models
{
    public class ImageFolder
    {
        public List<string> Images { get; set; } = new List<string>();
        public string Path { get; set; }
    }
}