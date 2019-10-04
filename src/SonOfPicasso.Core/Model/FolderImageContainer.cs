using System;
using System.Collections.Generic;
using System.Linq;
using SonOfPicasso.Data.Model;

namespace SonOfPicasso.Core.Model
{
    public class FolderImageContainer : ImageContainer
    {
        public FolderImageContainer(Folder folder)
        {
            Id = GetContainerId(folder);
            Name = folder.Path;
            Date = folder.Date;
            ImageRefs = folder.Images.Select(image => new ImageRef(image, this)).ToArray();
        }

        public override string Id { get; }
        public override string Name { get; }
        public override DateTime Date { get; }
        public override ImageContainerTypeEnum ContainerType => ImageContainerTypeEnum.Folder;
        public override IList<ImageRef> ImageRefs { get; }

        public static string GetContainerId(Folder folder)
        {
            return $"Folder{folder.Id}";
        }
    }
}