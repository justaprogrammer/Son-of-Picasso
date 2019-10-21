using System.Collections.Concurrent;
using System.Windows.Media.Imaging;
using SonOfPicasso.UI.Interfaces;

namespace SonOfPicasso.UI.Services
{
    public class SvgImageProvider: ISvgImageProvider
    {
        private readonly ConcurrentDictionary<string, BitmapImage> _concurrentDictionary;
        private readonly ISvgImageService _svgImageService;

        public SvgImageProvider(ISvgImageService svgImageService)
        {
            _svgImageService = svgImageService;
            _concurrentDictionary = new ConcurrentDictionary<string, System.Windows.Media.Imaging.BitmapImage>();
        }

        public BitmapImage Folder => GetOrAdd("FlatColorIcons.folder");

        public BitmapImage OpenedFolder => GetOrAdd("FlatColorIcons.opened_folder");

        public BitmapImage Cancel => GetOrAdd("FlatColorIcons.cancel");

        public BitmapImage Checkmark => GetOrAdd("FlatColorIcons.checkmark");

        public BitmapImage Synchronize => GetOrAdd("FlatColorIcons.synchronize");

        private BitmapImage GetOrAdd(string name)
        {
            return _concurrentDictionary.GetOrAdd(name, s => _svgImageService.LoadBitmapImage(s));
        }
    }
}