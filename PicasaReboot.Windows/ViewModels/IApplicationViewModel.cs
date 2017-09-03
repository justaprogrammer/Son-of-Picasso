using System.Collections.Generic;
using PicasaReboot.Core;

namespace PicasaReboot.Windows.ViewModels
{
    public interface IApplicationViewModel
    {
        IList<ImageViewModel> Images { get; set; }
    }
}