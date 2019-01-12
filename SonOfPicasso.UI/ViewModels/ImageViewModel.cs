using ReactiveUI;
using SonOfPicasso.Core.Models;

namespace SonOfPicasso.UI.ViewModels
{
    public class ImageViewModel : ReactiveObject, IImageViewModel
    {
        public Image Image { get; private set; }

        public void Initialize(Image image)
        {
            this.Image = image;
        }
    }
}