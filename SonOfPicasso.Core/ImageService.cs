using System;
using System.Windows.Media.Imaging;
using SonOfPicasso.Core.Helpers;
using SonOfPicasso.Core.Logging;
using SonOfPicasso.Core.Scheduling;

namespace SonOfPicasso.Core
{
    public class ImageService
    {
        private static ILogger Log { get; }= LogManager.ForContext<ImageService>();

        protected IFileSystem FileSystem { get; }

        public ISchedulerProvider Scheduler { get; }

        public ImageService(IFileSystem fileSystem, ISchedulerProvider scheduler)
        {
            Scheduler = scheduler;
            FileSystem = fileSystem;
        }

        public ImageService() : this(new FileSystem(), new SchedulerProvider())
        {
        }

        public string[] ListFiles(string directory)
        {
            Log.Debug("ListFiles: {Directory}", directory);

            Guard.NotNullOrEmpty(nameof(directory), directory);

            var strings = FileSystem.Directory.GetFiles(directory);

            var listFiles = strings
                .Select(s => new Tuple<string, string>(s, FileSystem.Path.GetExtension(s)))
                .Where(tuple => tuple.Item2 == ".jpg" || tuple.Item2 == ".jpeg" || tuple.Item2 == ".png")
                .Select(tuple => tuple.Item1).ToArray();

            return listFiles;
        }

        public IObservable<string[]> ListFilesAsync(string directory)
        {
            Log.Debug("ListFilesAsync: {Directory}", directory);

            return Observable.Create<string[]>(
                o => Observable.ToAsync<string, string[]>(ListFiles)(directory)
                    .SubscribeOn(Scheduler.ThreadPool)
                    .Subscribe(o));
        }

        public BitmapImage LoadImage(string path)
        {
            Log.Debug("LoadImage: {Path}", path);

            Guard.NotNullOrEmpty(nameof(path), path);

            var bytes = FileSystem.File.ReadAllBytes(path);
            return ImageHelpers.LoadBitmapImageFromBytes(bytes);
        }

        public IObservable<BitmapImage> LoadImageAsync(string path)
        {
            Log.Debug("LoadImageAsync: {Path}", path);

            Guard.NotNullOrEmpty(nameof(path), path);

            return Observable.Create<BitmapImage>(
                o => Observable.ToAsync<string, BitmapImage>(LoadImage)(path)
                    .SubscribeOn(Scheduler.ThreadPool)
                    .Subscribe(o));
        }
    }
}