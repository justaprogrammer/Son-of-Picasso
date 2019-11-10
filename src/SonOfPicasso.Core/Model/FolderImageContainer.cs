using System;
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
            Id = GetContainerId(folder);
            ContainerTypeId = folder.Id;
            Name = fileSystem.DirectoryInfo.FromDirectoryName(folder.Path).Name;
            Date = folder.Date;
            Year = folder.Date.Year;
            ImageRefs = folder.Images.Select(image => new ImageRef(image, this)).ToArray();
        }

        public int ContainerTypeId { get; }
        public string Id { get; }
        public string Name { get; }
        public int Year { get; }
        public DateTime Date { get; }
        public ImageContainerTypeEnum ContainerType => ImageContainerTypeEnum.Folder;
        public IList<ImageRef> ImageRefs { get; }

        public static string GetContainerId(Folder folder)
        {
            return $"Folder:{folder.Id}";
        }
    }
}