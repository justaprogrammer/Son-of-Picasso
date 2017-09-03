using System.Collections.Generic;
using PicasaReboot.Core;

namespace PicasaReboot.Windows.ViewModels
{
    public interface IApplicationViewModel
    {
        IList<ImageView> Images { get; set; }
    }
}