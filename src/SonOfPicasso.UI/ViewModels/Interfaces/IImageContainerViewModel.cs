using System;

namespace SonOfPicasso.UI.ViewModels.Interfaces
{
    public interface IImageContainerViewModel
    {
        string ContainerId { get; }
        ContainerTypeEnum ContainerType { get; }
        DateTime Date { get; }
    }

    public enum ContainerTypeEnum
    {
        Album,
        Folder
    }
}