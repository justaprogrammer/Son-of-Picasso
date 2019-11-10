using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive;
using System.Reactive.Linq;
using DynamicData;
using Serilog;
using SonOfPicasso.Core.Interfaces;
using SonOfPicasso.Core.Model;
using SonOfPicasso.Core.Scheduling;
using SonOfPicasso.Data.Model;

namespace SonOfPicasso.Core.Services
{
    public class ImageContainerImageManagementService : IImageContainerManagementService
    {
        private readonly IFolderRulesManagementService _folderRulesManagementService;
        private readonly IFolderWatcherService _folderWatcherService;

        private readonly SourceCache<IImageContainer, string> _imageContainerCache;
        private readonly IImageContainerOperationService _imageContainerOperationService;
        private readonly IObservableCache<ImageRef, string> _imageRefCache;
        private readonly ILogger _logger;
        private readonly ISchedulerProvider _schedulerProvider;
        private IDisposable _folderManagementDisposable;

        public ImageContainerImageManagementService(
            IImageContainerOperationService imageContainerOperationService,
            IFolderWatcherService folderWatcherService,
            IFolderRulesManagementService folderRulesManagementService,
            ISchedulerProvider schedulerProvider,
            ILogger logger)
        {
            _imageContainerOperationService = imageContainerOperationService;
            _folderWatcherService = folderWatcherService;
            _folderRulesManagementService = folderRulesManagementService;
            _schedulerProvider = schedulerProvider;
            _logger = logger;
            _imageContainerCache = new SourceCache<IImageContainer, string>(imageContainer => imageContainer.Id);
            _imageRefCache = _imageContainerCache
                .Connect()
                .ObserveOn(schedulerProvider.TaskPool)
                .TransformMany(container => container.ImageRefs, imageRef => imageRef.Id)
                .AsObservableCache();
        }

        public IConnectableCache<IImageContainer, string> ImageContainerCache => _imageContainerCache;

        public IConnectableCache<ImageRef, string> ImageRefCache => _imageRefCache;

        public void Dispose()
        {
            _folderManagementDisposable?.Dispose();
            _imageContainerCache?.Dispose();
            _imageRefCache?.Dispose();
        }

        public IObservable<Unit> Start()
        {
            return _imageContainerOperationService.GetAllImageContainers()
                .Do(container => _imageContainerCache.AddOrUpdate(container))
                .LastOrDefaultAsync()
                .Select(_ => Unit.Default)
                .Do(_ => StartWatcher());
        }

        public void Stop()
        {
            _folderManagementDisposable?.Dispose();
            _folderManagementDisposable = null;
        }

        public IObservable<IImageContainer> ScanFolder(string path)
        {
            return _imageContainerOperationService.ScanFolder(path)
                .Do(container => _imageContainerCache.AddOrUpdate(container))
                .ToArray()
                .SelectMany(async containers =>
                {
                    await _folderRulesManagementService
                        .AddFolderManagementRule(
                            new FolderRule
                            {
                                Path = path,
                                Action = FolderRuleActionEnum.Once
                            });

                    return containers;
                })
                .SelectMany(containers => containers);
        }

        public IObservable<IImageContainer> CreateAlbum(ICreateAlbum createAlbum)
        {
            return _imageContainerOperationService.CreateAlbum(createAlbum)
                .Do(container => _imageContainerCache.AddOrUpdate(container));
        }

        public IObservable<IImageContainer> AddImagesToAlbum(int albumId, IEnumerable<int> imageIds)
        {
            return _imageContainerOperationService.AddImagesToAlbum(albumId, imageIds)
                .Do(container => _imageContainerCache.AddOrUpdate(container));
        }

        public IObservable<IImageContainer> AddImage(string path)
        {
            return _imageContainerOperationService.AddImage(path)
                .Do(container => _imageContainerCache.AddOrUpdate(container));
        }

        public IObservable<IImageContainer> DeleteImage(string path)
        {
            return _imageContainerOperationService.DeleteImage(path)
                .Do(container => _imageContainerCache.AddOrUpdate(container));
        }

        public IObservable<IImageContainer> RenameImage(string oldPath, string newPath)
        {
            return _imageContainerOperationService.RenameImage(oldPath, newPath)
                .Do(container => _imageContainerCache.AddOrUpdate(container));
        }

        public IObservable<Unit> DeleteAlbum(int albumId)
        {
            return _imageContainerOperationService.DeleteAlbum(albumId)
                .Do(container => _imageContainerCache.Remove(AlbumImageContainer.GetContainerId(albumId)));
        }

        private void StartWatcher()
        {
            _folderManagementDisposable = _folderRulesManagementService.GetFolderManagementRules()
                .SelectMany(list => _folderWatcherService.WatchFolders(list))
                .Subscribe(HandlerFolderWatcherEvent);
        }

        private void HandlerFolderWatcherEvent(FileSystemEventArgs args)
        {
            _logger.Debug("HandlerFolderWatcherEvent {ChangeType} {Name}", args.ChangeType, args.Name);

            switch (args.ChangeType)
            {
                case WatcherChangeTypes.Created:
                    break;

                case WatcherChangeTypes.Changed:
                    _imageContainerCache.PopulateFrom(
                        _imageContainerOperationService.AddOrUpdateImage(args.FullPath));
                    break;

                case WatcherChangeTypes.Deleted:
                    _imageContainerCache.PopulateFrom(
                        _imageContainerOperationService.DeleteImage(args.FullPath));
                    break;

                case WatcherChangeTypes.Renamed:
                    var renamedEventArgs = (RenamedEventArgs) args;
                    _imageContainerCache.PopulateFrom(
                        _imageContainerOperationService.RenameImage(renamedEventArgs.OldFullPath,
                            renamedEventArgs.FullPath));
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}