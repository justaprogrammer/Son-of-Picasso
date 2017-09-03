using System;
using System.IO.Abstractions;

namespace PicasaReboot.Core
{
    public class ImageFileSystemService
    {
        protected IFileSystem FileSystem { get; }

        public ImageFileSystemService(IFileSystem fileSystem)
        {
            FileSystem = fileSystem;
        }

        public string LibraryDirectory { get; set; }

        public IObservable<ImageFile> ListFiles()
        {
            throw new NotImplementedException();
        }
    }
}