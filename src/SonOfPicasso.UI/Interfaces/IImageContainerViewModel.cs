using System;
using System.ComponentModel;
using DynamicData.Binding;
using SonOfPicasso.Core.Model;
using SonOfPicasso.UI.ViewModels;

namespace SonOfPicasso.UI.Interfaces
{
    public interface IImageContainerViewModel: INotifyPropertyChanged
    {
        string Name { get; }
        string ContainerId { get; }
        ImageContainerTypeEnum ContainerType { get; }
        DateTime Date { get; }
        IObservableCollection<ImageRowViewModel> ImageRowViewModels { get; }
        IApplicationViewModel ApplicationViewModel { get; }
        ImageRowViewModel SelectedImageRow { get; }
        ImageViewModel SelectedImage { get; }
    }
}