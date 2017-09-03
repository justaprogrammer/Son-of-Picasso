﻿using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Media.Imaging;
using PicasaReboot.Core.Helpers;

namespace PicasaReboot.Core
{
    public class ImageFileSystemService
    {
        protected IFileSystem FileSystem { get; }

        public ImageFileSystemService(IFileSystem fileSystem)
        {
            FileSystem = fileSystem;
        }

        public ImageFileSystemService() : this(new FileSystem())
        {
        }

        public string[] ListFiles(string directory)
        {
            Guard.NotNullOrEmpty(nameof(directory), directory);

            var strings = FileSystem.Directory.GetFiles(directory);

            return strings
                .Select(s => new Tuple<string, string>(s, FileSystem.Path.GetExtension(s)))
                .Where(tuple => tuple.Item2 == ".jpg" || tuple.Item2 == ".jpeg" || tuple.Item2 == ".png")
                .Select(tuple => tuple.Item1).ToArray();
        }

        public BitmapImage LoadImage(string path)
        {
            Guard.NotNullOrEmpty(nameof(path), path);

            var bytes = FileSystem.File.ReadAllBytes(path);
            return ImageHelpers.LoadImage(bytes);
        }
    }
}