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
        private readonly IDataCache _dataCache;
        private readonly IImageLocationService _imageLocationService;
        private readonly ILogger<ImageManagementService> _logger;

        public ImageManagementService(IDataCache dataCache,
            IImageLocationService imageLocationService,
            ILogger<ImageManagementService>logger)
        {
            _dataCache = dataCache ?? throw new ArgumentNullException(nameof(dataCache));
            _imageLocationService = imageLocationService ?? throw new ArgumentNullException(nameof(imageLocationService));
            _logger = logger;
        }

        public IObservable<Unit> AddFolder(string path)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));

            _logger.LogDebug("AddFolder {Path}", path);

            return _dataCache.GetFolderList()
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

                    var setFolderList = _dataCache.SetFolderList(currentFolders.Append(path).ToArray());
                    var setFolder = _dataCache.SetFolder(
                        new ImageFolderModel {Path = path, Images = folderImages});

                    return setFolderList
                        .SelectMany(setFolder)
                        .ToArray()
                        .Select(_ => Unit.Default);
                });
        }

        public IObservable<Unit> RemoveFolder(string path)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));

            _logger.LogDebug("RemoveFolder {Path}", path);

            return _dataCache.GetFolderList()
                .Select(folders =>
                {
                    if (!folders.Contains(path))
                    {
                        throw new SonOfPicassoException("Folder does not exist");
                    }

                    var currentFolders = folders.Where(s => s != path).ToArray();

                    var setFolderList = _dataCache.SetFolderList(currentFolders);
                    var setFolder = _dataCache.DeleteFolder(path);

                    return setFolderList
                        .SelectMany(setFolder)
                        .ToArray()
                        .Select(_ => Unit.Default);
                })
                .SelectMany(observable => observable);
        }

        public IObservable<ImageFolderModel> GetAllImageFolders()
        {
            return _dataCache.GetFolderList()
                .SelectMany(strings => strings)
                .SelectMany(s => _dataCache.GetFolder(s));
        }

        public IObservable<ImageModel> GetAllImages()
        {
            return this.GetAllImageFolders()
                .SelectMany(imageFolder => imageFolder.Images)
                .SelectMany(imagePath => _dataCache.GetImage(imagePath));
        }
    }
}