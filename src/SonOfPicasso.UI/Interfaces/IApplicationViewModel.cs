using System.ComponentModel;
using SonOfPicasso.UI.ViewModels;

namespace SonOfPicasso.UI.Interfaces
{
    public interface IApplicationViewModel: INotifyPropertyChanged
    {
        IImageViewModel SelectedItem { get; set; }
        IImageRowViewModel SelectedRow { get; set; }
    }
}