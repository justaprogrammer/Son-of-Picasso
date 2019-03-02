using System;
using System.Reactive;
using Akavache;
using Microsoft.Extensions.Logging;
using SonOfPicasso.Core.Interfaces;
using SonOfPicasso.Core.Models;

namespace SonOfPicasso.Core.Services
{
    public class DataCache : IDataCache
    {
        private const string UserSettingsKey = "UserSettings";
        private const string ImageFoldersKey = "ImageFolders";

        private readonly ILogger<DataCache> _logger;
        protected readonly IBlobCache BlobCache;

        static DataCache()
        {
            Akavache.BlobCache.ApplicationName = "SonOfPicasso";
        }

        public DataCache(ILogger<DataCache> logger) : this(logger, null)
        {
        }

        internal DataCache(ILogger<DataCache> logger,
            IBlobCache blobCache)
        {
            _logger = logger;
            if (blobCache == null)
            {
                try
                {
                    blobCache = Akavache.BlobCache.UserAccount;
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Failed to get the UserAccount cache");
                    throw;
                }
            }

            BlobCache = blobCache;
        }

        public IObservable<Unit> DeleteImage(string path)
        {
            throw new NotImplementedException();
        }

        public IObservable<Unit> Clear()
        {
            return BlobCache.InvalidateAll();
        }

        public IObservable<UserSettings> GetUserSettings()
        {
            _logger.LogDebug("GetUserSettings");
            return BlobCache.GetOrCreateObject(UserSettingsKey, CreateUserSettings);
        }

        public IObservable<Unit> SetUserSettings(UserSettings userSettings)
        {
            _logger.LogDebug("SetUserSettings");
            return BlobCache.InsertObject(UserSettingsKey, userSettings);
        }

        private static UserSettings CreateUserSettings()
        {
            return new UserSettings();
        }

        public IObservable<string[]> GetFolderList()
        {
            _logger.LogDebug("GetFolderList");
            return BlobCache.GetOrCreateObject(ImageFoldersKey, Array.Empty<string>);
        }

        public IObservable<Unit> SetFolderList(string[] paths)
        {
            _logger.LogDebug("SetFolderList");
            return BlobCache.InsertObject(ImageFoldersKey, paths);
        }

        public IObservable<Unit> DeleteFolder(string path)
        {
            _logger.LogDebug("GetFolder");
            return BlobCache.Invalidate(GetImageFolderKey(path));
        }

        public IObservable<ImageModel> GetImage(string path)
        {
            _logger.LogDebug("GetFolder");
            return BlobCache.GetObject<ImageModel>(GetImageKey(path));
        }

        public IObservable<Unit> SetImage(ImageModel image)
        {
            _logger.LogDebug("SetImage");
            return BlobCache.InsertObject(GetImageKey(image.Path), image);
        }

        public IObservable<ImageFolderModel> GetFolder(string path)
        {
            _logger.LogDebug("GetFolder");
            return BlobCache.GetOrCreateObject(GetImageFolderKey(path), () => CreateImageFolder(path));
        }

        private static ImageFolderModel CreateImageFolder(string path)
        {
            return new ImageFolderModel
            {
                Path = path
            };
        }

        public IObservable<Unit> SetFolder(ImageFolderModel imageFolder)
        {
            _logger.LogDebug("SetFolder");
            return BlobCache.InsertObject(GetImageFolderKey(imageFolder.Path), imageFolder);
        }

        private static string GetImageFolderKey(string path) => $"ImageFolder {path}";
        private static string GetImageKey(string path) => $"Image {path}";
    }
}