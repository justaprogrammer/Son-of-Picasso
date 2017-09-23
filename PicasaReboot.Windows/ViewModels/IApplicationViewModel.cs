using System.Collections.Generic;
using System.Collections.ObjectModel;
using PicasaReboot.Core;
using ReactiveUI;

namespace PicasaReboot.Windows.ViewModels
{
    public interface IApplicationViewModel
    {
        string Directory { get; set; }
        ReactiveList<IImageViewModel> Images { get; }
        ImageService ImageService { get; }
    }
}