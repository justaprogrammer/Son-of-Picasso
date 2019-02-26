using System;
using System.Collections.Generic;

namespace SonOfPicasso.Core.Models
{
    public class ImageFolder
    {
        public string[] Images { get; set; } = Array.Empty<string>();
        public string Path { get; set; }
    }
}