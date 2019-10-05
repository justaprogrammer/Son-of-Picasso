using System.ComponentModel;
using SonOfPicasso.Core.Model;

namespace SonOfPicasso.UI.Interfaces
{
    public interface IImageViewModel: INotifyPropertyChanged
    {
        IImageRowViewModel ImageRowViewModel { get; }
        ImageRef ImageRef { get; }
        int ImageId { get; }
        string Path { get; }
        string ImageRefId { get; }
    }
}