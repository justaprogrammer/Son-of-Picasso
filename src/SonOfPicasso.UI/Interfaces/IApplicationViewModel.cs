using System.ComponentModel;
using SonOfPicasso.UI.ViewModels;

namespace SonOfPicasso.UI.Interfaces
{
    public interface IApplicationViewModel: INotifyPropertyChanged
    {
        ImageViewModel SelectedItem { get; set; }
    }
}