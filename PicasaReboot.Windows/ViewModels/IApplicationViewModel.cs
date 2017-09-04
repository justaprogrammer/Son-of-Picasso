using System.Collections.Generic;
using System.Collections.ObjectModel;
using PicasaReboot.Core;

namespace PicasaReboot.Windows.ViewModels
{
    public interface IApplicationViewModel
    {
        ObservableCollection<ImageViewModel> Images { get; }
    }
}