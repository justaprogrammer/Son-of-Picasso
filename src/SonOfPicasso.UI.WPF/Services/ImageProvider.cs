using System.Collections.Concurrent;
using System.Windows.Media.Imaging;
using SonOfPicasso.Core.Interfaces;
using SonOfPicasso.UI.WPF.Extensions;
using SonOfPicasso.UI.WPF.Interfaces;

namespace SonOfPicasso.UI.WPF.Services
{
    public class ImageProvider: IImageProvider
    {
        private readonly ConcurrentDictionary<string, BitmapImage> _concurrentDictionary;
        private readonly ISvgLoadingService _svgLoadingService;

        public ImageProvider(ISvgLoadingService svgLoadingService)
        {
            _svgLoadingService = svgLoadingService;
            _concurrentDictionary = new ConcurrentDictionary<string, BitmapImage>();
        }

        public BitmapImage Folder => GetOrAddSvg("FlatColorIcons.folder");

        public BitmapImage OpenedFolder => GetOrAddSvg("FlatColorIcons.opened_folder");

        public BitmapImage Cancel => GetOrAddSvg("FlatColorIcons.cancel");

        public BitmapImage Checkmark => GetOrAddSvg("FlatColorIcons.checkmark");

        public BitmapImage Synchronize => GetOrAddSvg("FlatColorIcons.synchronize");

        private BitmapImage GetOrAddSvg(string name)
        {
            return _concurrentDictionary.GetOrAdd(name, s =>
            {
                using var bitmap = _svgLoadingService.Load($"SonOfPicasso.UI.Resources.{s}.svg", typeof(ImageProvider));
                return bitmap.ToBitmapImage();
            });
        }
    }
}