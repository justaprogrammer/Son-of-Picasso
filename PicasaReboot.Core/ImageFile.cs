using System;
using System.Reactive.Linq;

namespace PicasaReboot.Core
{
    public class ImageFile
    {
        public string Path;

        public ImageFile(ImageFileSystemService fileSystem, string path)
        {
            Path = path;
        }
    }
}
