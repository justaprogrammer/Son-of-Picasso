using System;
using System.Reactive;
using SonOfPicasso.Core.Interfaces;

namespace SonOfPicasso.Core.Services
{
    public class ImageManagementService : IImageManagementService
    {
        private readonly ISharedCache _sharedCache;
        private readonly IImageLocationService _imageLocationService;

        public ImageManagementService(ISharedCache sharedCache, IImageLocationService imageLocationService)
        {
            _sharedCache = sharedCache;
            _imageLocationService = imageLocationService;
        }

        public IObservable<Unit> AddFolder(string path)
        {
            throw new NotImplementedException();
        }

        public IObservable<Unit> RemoveFolder(string path)
        {
            throw new NotImplementedException();
        }
    }
}