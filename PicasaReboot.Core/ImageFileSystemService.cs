using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Reactive.Linq;

namespace PicasaReboot.Core
{
    public class ImageFileSystemService
    {
        protected IFileSystem FileSystem { get; }

        public ImageFileSystemService(IFileSystem fileSystem)
        {
            FileSystem = fileSystem;
        }

        public IReadOnlyList<ImageFile> ListFiles(string directory)
        {
            Guard.NotNullOrEmpty(nameof(directory), directory);

            var strings = FileSystem.Directory.GetFiles(directory);

            return strings
                .Where(s => FileSystem.Path.GetExtension(s) == ".jpg")
                .Select(CreateImageFileFromPath).ToArray();
        }

        private ImageFile CreateImageFileFromPath(string path)
        {
            return new ImageFile(this, path);
        }
    }
}