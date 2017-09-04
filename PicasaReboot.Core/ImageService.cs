using System;
using System.IO.Abstractions;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Media.Imaging;
using PicasaReboot.Core.Helpers;
using Serilog;

namespace PicasaReboot.Core
{
    public class ImageService
    {
        private static ILogger Log { get; }= LogManager.ForContext<ImageService>();

        protected IFileSystem FileSystem { get; }

        public ImageService(IFileSystem fileSystem)
        {
            FileSystem = fileSystem;
        }

        public ImageService() : this(new FileSystem())
        {
        }

        public string[] ListFiles(string directory)
        {
            Log.Debug("ListFiles: {directory}", directory);

            Guard.NotNullOrEmpty(nameof(directory), directory);

            var strings = FileSystem.Directory.GetFiles(directory);

            return strings
                .Select(s => new Tuple<string, string>(s, FileSystem.Path.GetExtension(s)))
                .Where(tuple => tuple.Item2 == ".jpg" || tuple.Item2 == ".jpeg" || tuple.Item2 == ".png")
                .Select(tuple => tuple.Item1).ToArray();
        }

        public IObservable<string[]> ListFilesAsync(string directory)
        {
            Log.Debug("ListFilesAsync: {directory}", directory);

            return Observable.Create<string[]>(
                o => Observable.ToAsync<string, string[]>(ListFiles)(directory).Subscribe(o)
            );
        }

        public BitmapImage LoadImage(string path)
        {
            Log.Debug("LoadImage: {path}", path);

            Guard.NotNullOrEmpty(nameof(path), path);

            var bytes = FileSystem.File.ReadAllBytes(path);
            return ImageHelpers.LoadBitmapImageFromBytes(bytes);
        }

        public IObservable<BitmapImage> LoadImageAsync(string path)
        {
            Log.Debug("LoadImageAsync: {path}", path);

            return Observable.Create<BitmapImage>(
                o => Observable.ToAsync<string, BitmapImage>(LoadImage)(path).Subscribe(o)
            );
        }
    }
}