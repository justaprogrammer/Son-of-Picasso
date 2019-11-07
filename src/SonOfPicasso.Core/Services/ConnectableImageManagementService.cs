using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using DynamicData;
using SonOfPicasso.Core.Interfaces;
using SonOfPicasso.Core.Model;
using SonOfPicasso.Core.Scheduling;

namespace SonOfPicasso.Core.Services
{
    public class ConnectableImageManagementService : IConnectableManagementService, IDisposable
    {
        private readonly IImageManagementService _imageManagementService;
        private readonly IFolderWatcherService _folderWatcherService;

        private readonly SourceCache<IImageContainer, string> _imageContainerCache;
        private readonly IObservableCache<ImageRef, string> _imageRefCache;

        public ConnectableImageManagementService(IImageManagementService imageManagementService,
            IFolderWatcherService folderWatcherService,
            ISchedulerProvider schedulerProvider)
        {
            _imageManagementService = imageManagementService;
            _folderWatcherService = folderWatcherService;
            _imageContainerCache = new SourceCache<IImageContainer, string>(imageContainer => imageContainer.Id);
            _imageRefCache = _imageContainerCache
                .Connect()
                .ObserveOn(schedulerProvider.TaskPool)
                .TransformMany(container => container.ImageRefs, imageRef => imageRef.Id)
                .AsObservableCache();
        }

        public IConnectableCache<IImageContainer, string> ImageContainerCache => _imageContainerCache;

        public IConnectableCache<ImageRef, string> ImageRefCache => _imageRefCache;

        public IObservable<Unit> Start()
        {
            return _imageManagementService.GetAllImageContainers()
                .Do(container => _imageContainerCache.AddOrUpdate(container))
                .Select(container => Unit.Default)
                .LastOrDefaultAsync();
        }

        public void Stop()
        {
        }

        public void Dispose()
        {
            _imageContainerCache?.Dispose();
            _imageRefCache?.Dispose();
        }

        public IObservable<Unit> ScanFolder(string path)
        {
            return _imageManagementService.ScanFolder(path)
                .Select(container =>
                {
                    var containerImageRefs = container.ImageRefs;

                    _imageContainerCache.AddOrUpdate(container);
                    return Unit.Default;
                })
                .LastAsync();
        }
    }
}