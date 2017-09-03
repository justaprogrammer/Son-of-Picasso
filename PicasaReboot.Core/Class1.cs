using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PicasaReboot.Core
{
    public class ImageFileSystemService
    {
        protected IFileSystem FileSystem { get; }

        public ImageFileSystemService(IFileSystem fileSystem)
        {
            FileSystem = fileSystem;
        }
    }
}
