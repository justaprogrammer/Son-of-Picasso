using System.Collections.Generic;
using PicasaReboot.Core;
using ReactiveUI;

namespace PicasaReboot.Windows.ViewModels
{
    public class ApplicationViewModel : ReactiveObject, IApplicationViewModel
    {
        private IList<ImageView> _images;

        public IList<ImageView> Images
        {
            get { return _images; }
            set { this.RaiseAndSetIfChanged(ref _images, value); }
        }
    }
}
