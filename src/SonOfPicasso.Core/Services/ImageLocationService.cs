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
        private static readonly string[] ImageExtensions = { ".jpg", ".jpeg", ".png", ".tif", ".tiff", ".bmp" };

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

        public IObservable<string[]> GetImages(string path)
        {
            return Observable.Start(() =>
            {
                _logger.Debug("GetImages {Path}", path);

                var fileInfoBases = Array.Empty<string>();
                try
                {
                    fileInfoBases = _fileSystem.DirectoryInfo.FromDirectoryName(path)
                        .EnumerateFiles("*.*", SearchOption.AllDirectories)
                        .Where(file => ImageExtensions.Contains(file.Extension.ToLowerInvariant()))
                        .Select(file => file.FullName)
                        .ToArray();
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Error GetImages {Path}", path);
                }

                return fileInfoBases;
            }, _schedulerProvider.TaskPool);
        }
    }
}
