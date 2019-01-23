using System.Globalization;
using System.Reactive.Linq;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using ReactiveUI;
using SonOfPicasso.Core.Models;
using SonOfPicasso.Core.Scheduling;
using SonOfPicasso.UI.Injection;
using SonOfPicasso.UI.Interfaces;
using SonOfPicasso.UI.Views;

namespace SonOfPicasso.UI.ViewModels
{
    [ViewModelView(typeof(ImageViewControl))]
    public class ImageViewModel : ReactiveObject, IImageViewModel
    {
        public void Initialize(Image image)
        {
            this.Image = image;
        }

        private Image _image;

        public Image Image
        {
            get => _image;
            set => this.RaiseAndSetIfChanged(ref _image, value);
        }
    }
}