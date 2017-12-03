using System.Collections.Generic;
using System.Collections.ObjectModel;
using PicasaReboot.Core;
using ReactiveUI;

namespace PicasaReboot.Windows.ViewModels
{
    public interface IDirectoryViewModel
    {
        string Name { get; set; }
        ReactiveList<IImageViewModel> Images { get; }
    }
}