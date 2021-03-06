﻿using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using SonOfPicasso.Data.Model;

namespace SonOfPicasso.Core.Model
{
    public class FolderImageContainer : IImageContainer
    {
        public FolderImageContainer(Folder folder, IFileSystem fileSystem)
        {
            Key = GetContainerKey(folder);
            Id = folder.Id;
            Name = fileSystem.DirectoryInfo.FromDirectoryName(folder.Path).Name;
            Date = folder.Date;
            Year = folder.Date.Year;
            ImageRefs = folder.Images.Select(image => ImageRef.CreateImageRef(image, this)).ToArray();
        }

        public int Id { get; }
        public string Key { get; }
        public string Name { get; }
        public int Year { get; }
        public DateTime Date { get; }
        public ImageContainerTypeEnum ContainerType => ImageContainerTypeEnum.Folder;
        public IList<ImageRef> ImageRefs { get; }

        public static string GetContainerKey(Folder folder)
        {
            var folderId = folder.Id;
            return GetContainerKey(folderId);
        }

        public static string GetContainerKey(int folderId)
        {
            return $"Folder:{folderId}";
        }
    }
}