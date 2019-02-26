using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;
using Akavache;
using Microsoft.Extensions.Logging;
using SonOfPicasso.Core.Interfaces;
using SonOfPicasso.Core.Models;

namespace SonOfPicasso.Core.Services
{
    public class SharedCache : ISharedCache
    {
        private const string UserSettingsKey = "UserSettings";
        private const string ImageFoldersKey = "ImageFolders";

        private readonly ILogger<SharedCache> _logger;
        protected readonly IBlobCache BlobCache;

        static SharedCache()
        {
            Akavache.BlobCache.ApplicationName = "SonOfPicasso";
        }

        public SharedCache(ILogger<SharedCache> logger) : this(logger, null)
        {
        }

        internal SharedCache(ILogger<SharedCache> logger,
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

        public IObservable<bool> FolderExists(string path)
        {
            _logger.LogDebug("FolderExists");

            return BlobCache.Get(GetImageFolderKey(path))
                .Select(_ => true)
                .Catch<bool, KeyNotFoundException>(_ => Observable.Return(false));
        }

        public IObservable<ImageFolder> GetFolder(string path)
        {
            _logger.LogDebug("GetFolder");
            return BlobCache.GetOrCreateObject(GetImageFolderKey(path), () => CreateImageFolder(path));
        }

        private static ImageFolder CreateImageFolder(string path)
        {
            return new ImageFolder
            {
                Path = path
            };
        }

        public IObservable<Unit> SetFolder(ImageFolder imageFolder)
        {
            _logger.LogDebug("SetFolder");
            return BlobCache.InsertObject(GetImageFolderKey(imageFolder.Path), imageFolder);
        }

        private static string GetImageFolderKey(string path) => $"ImageFolder {path}";
    }
}