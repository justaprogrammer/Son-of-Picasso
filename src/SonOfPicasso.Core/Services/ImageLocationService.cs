using System;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using Serilog;
using SonOfPicasso.Core.Interfaces;
using SonOfPicasso.Core.Scheduling;

namespace SonOfPicasso.Core.Services
{
    public class ImageLocationService: IImageLocationService
    {
        private readonly IFileSystem _fileSystem;
        private readonly ISchedulerProvider _schedulerProvider;
        private readonly ILogger _logger;

        public ImageLocationService(ILogger logger, 
            IFileSystem fileSystem, 
            ISchedulerProvider schedulerProvider)
        {
            _logger = logger;
            _fileSystem = fileSystem;
            _schedulerProvider = schedulerProvider;
        }

        public IObservable<IFileInfo> GetImages(string path)
        {
            return Observable.Defer(() =>
            {
                _logger.Verbose("GetImages {Path}", path);

                return _fileSystem.DirectoryInfo.FromDirectoryName(path)
                        .EnumerateFiles("*.*", SearchOption.AllDirectories)
                        .Where(file => Constants.ImageExtensions.Contains(file.Extension.ToLowerInvariant()))
                        .ToObservable();
            }).SubscribeOn(_schedulerProvider.TaskPool);
        }
    }
}
