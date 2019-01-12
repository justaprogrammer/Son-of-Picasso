using System;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using Microsoft.Extensions.Logging;
using SonOfPicasso.Core.Interfaces;

namespace SonOfPicasso.Core.Services
{
    public class ImageLocationService: IImageLocationService
    {
        private readonly IFileSystem _fileSystem;
        private readonly ILogger<ImageLocationService> _logger;
        private static readonly string[] ImageExtensions = new[] { ".jpg", ".jpeg", ".tiff", ".bmp" };

        public ImageLocationService(ILogger<ImageLocationService> logger, IFileSystem fileSystem)
        {
            _logger = logger;
            _fileSystem = fileSystem;
        }

        public IObservable<FileInfoBase[]> GetImages(string path)
        {
            _logger.LogDebug("GetImages {Path}", path);

            var fileInfoBases = Array.Empty<FileInfoBase>();
            try
            {
                fileInfoBases = _fileSystem.DirectoryInfo.FromDirectoryName(path)
                    .EnumerateFiles("*.*", SearchOption.AllDirectories)
                    .Where(file => ImageExtensions.Contains(file.Extension.ToLowerInvariant()))
                    .ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error GetImages {Path}", path);
            }

            return Observable.Return(fileInfoBases);
        }
    }
}
