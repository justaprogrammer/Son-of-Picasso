using System.ComponentModel;
using SonOfPicasso.UI.ViewModels;

namespace SonOfPicasso.UI.Interfaces
{
    public interface IApplicationViewModel: INotifyPropertyChanged
    {
        ImageContainerViewModel SelectedImageContainer { get; }
        ImageRowViewModel SelectedImageRow { get; }
        ImageViewModel SelectedImage { get; }
    }
}