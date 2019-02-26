using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using Microsoft.Extensions.Logging;
using SonOfPicasso.Core.Interfaces;
using SonOfPicasso.Core.Models;

namespace SonOfPicasso.Core.Services
{
    public class ImageManagementService : IImageManagementService
    {
        private readonly ISharedCache _sharedCache;
        private readonly IImageLocationService _imageLocationService;
        private readonly ILogger<ImageManagementService> _logger;

        public ImageManagementService(ISharedCache sharedCache,
            IImageLocationService imageLocationService,
            ILogger<ImageManagementService>logger)
        {
            _sharedCache = sharedCache ?? throw new ArgumentNullException(nameof(sharedCache));
            _imageLocationService = imageLocationService ?? throw new ArgumentNullException(nameof(imageLocationService));
            _logger = logger;
        }

        public IObservable<Unit> AddFolder(string path)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));

            _logger.LogDebug("AddFolder {Path}", path);

            return _sharedCache.GetFolderList()
                .Select(folders =>
                {
                    if (folders.Contains(path))
                    {
                        throw new SonOfPicassoException("Folder already exists");
                    }

                    return _imageLocationService
                        .GetImages(path)
                        .Select(images => (folders, images));
                })
                .SelectMany(obs => obs)
                .SelectMany(elements =>
                {
                    var currentFolders = elements.Item1;
                    var folderImages = elements.Item2;

                    var setFolderList = _sharedCache.SetFolderList(currentFolders.Append(path).ToArray());
                    var setFolder = _sharedCache.SetFolder(
                        new ImageFolder {Path = path, Images = folderImages});

                    return setFolderList
                        .SelectMany(setFolder)
                        .ToArray()
                        .Select(_ => Unit.Default);
                });
        }

        public IObservable<Unit> RemoveFolder(string path)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));

            throw new NotImplementedException();
        }
    }
}