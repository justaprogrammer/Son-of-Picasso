using System.Collections.Generic;
using System.ComponentModel;
using SonOfPicasso.UI.ViewModels;

namespace SonOfPicasso.UI.Interfaces
{
    public interface IImageRowViewModel: INotifyPropertyChanged
    {
        IImageContainerViewModel ImageContainerViewModel { get; }
        IList<ImageViewModel> ImageViewModels { get; }
    }
}