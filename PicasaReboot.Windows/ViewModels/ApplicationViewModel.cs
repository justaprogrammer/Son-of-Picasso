using System.Collections.Generic;
using PicasaReboot.Core;
using ReactiveUI;

namespace PicasaReboot.Windows.ViewModels
{
    public class ApplicationViewModel : ReactiveObject, IApplicationViewModel
    {
        private IList<string> _images;

        public IList<string> Images
        {
            get { return _images; }
            set { this.RaiseAndSetIfChanged(ref _images, value); }
        }
    }
}
