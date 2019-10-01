using System;
using System.Collections.Generic;

namespace SonOfPicasso.UI.ViewModels.Interfaces
{
    public interface IImageContainerViewModel
    {
        string ContainerId { get; }
        ContainerTypeEnum ContainerType { get; }
        DateTime Date { get; }
        IList<int> ImageIds { get; }
    }

    public enum ContainerTypeEnum
    {
        Album,
        Folder
    }
}