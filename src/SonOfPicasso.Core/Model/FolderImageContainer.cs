using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using Microsoft.VisualBasic;
using SonOfPicasso.Data.Model;

namespace SonOfPicasso.Core.Model
{
    public class FolderImageContainer : ImageContainer
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

        public override int ContainerTypeId { get; }
        public override string Id { get; }
        public override string Name { get; }
        public override int Year { get; }
        public override DateTime Date { get; }
        public override ImageContainerTypeEnum ContainerType => ImageContainerTypeEnum.Folder;
        public override IList<ImageRef> ImageRefs { get; }

        public static string GetContainerId(Folder folder)
        {
            return $"Folder:{folder.Id}";
        }
    }
}