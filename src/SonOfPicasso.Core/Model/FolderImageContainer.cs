using System;
using SonOfPicasso.Data.Model;

namespace SonOfPicasso.Core.Model
{
    public class FolderImageContainer : ImageContainer
    {
        private readonly Folder _folder;

        public FolderImageContainer(Folder folder)
        {
            _folder = folder;
        }

        public override string Id => GetContainerId(_folder);
        public override string Name => _folder.Path;
        public override DateTime Date => _folder.Date;
        public override ImageContainerTypeEnum ContainerType => ImageContainerTypeEnum.Folder;

        public static string GetContainerId(Folder folder)
        {
            return $"Folder{folder.Id}";
        }
    }
}