using System.Windows.Media.Imaging;
using ReactiveUI;

namespace PicasaReboot.Windows.ViewModels
{
    public class ImageView: ReactiveObject, IImageView
    {
        public string File { get; }
        public BitmapImage Image { get; }

        public ImageView(string file, BitmapImage image)
        {
            File = file;
            Image = image;
        }
    }
}