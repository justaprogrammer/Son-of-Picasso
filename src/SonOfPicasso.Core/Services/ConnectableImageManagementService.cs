using System;
using System.IO;
using System.Reactive;
using System.Reactive.Linq;
using DynamicData;
using SonOfPicasso.Core.Interfaces;
using SonOfPicasso.Core.Model;
using SonOfPicasso.Core.Scheduling;
using SonOfPicasso.Data.Model;

namespace SonOfPicasso.Core.Services
{
    public class ConnectableImageManagementService : IConnectableManagementService, IDisposable
    {
        private readonly IFolderRulesManagementService _folderRulesManagementService;
        private readonly IFolderWatcherService _folderWatcherService;

        private readonly SourceCache<IImageContainer, string> _imageContainerCache;
        private readonly IImageManagementService _imageManagementService;
        private readonly IObservableCache<ImageRef, string> _imageRefCache;
        private IDisposable folderManagementDisposable;

        public ConnectableImageManagementService(IImageManagementService imageManagementService,
            IFolderWatcherService folderWatcherService,
            IFolderRulesManagementService folderRulesManagementService,
            ISchedulerProvider schedulerProvider)
        {
            _imageManagementService = imageManagementService;
            _folderWatcherService = folderWatcherService;
            _folderRulesManagementService = folderRulesManagementService;
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
            folderManagementDisposable?.Dispose();
            _imageContainerCache?.Dispose();
            _imageRefCache?.Dispose();
        }

        public IObservable<Unit> Start()
        {
            return _imageManagementService.GetAllImageContainers()
                .Do(container => _imageContainerCache.AddOrUpdate(container))
                .LastOrDefaultAsync()
                .Select(_ => Unit.Default)
                .Do(_ => StartWatcher());
        }

        private void StartWatcher()
        {
            folderManagementDisposable = _folderRulesManagementService.GetFolderManagementRules()
                .SelectMany(list => _folderWatcherService.WatchFolders(list))
                .Subscribe(HandlerFolderWatcherEvent);
        }

        private void HandlerFolderWatcherEvent(FileSystemEventArgs args)
        {
            switch (args.ChangeType)
            {
                case WatcherChangeTypes.Created:
                    _imageContainerCache.PopulateFrom(_imageManagementService.AddImage(args.FullPath));
                    break;

                case WatcherChangeTypes.Deleted:
                    _imageContainerCache.PopulateFrom(_imageManagementService.DeleteImage(args.FullPath));
                    break;

                case WatcherChangeTypes.Changed:
                    _imageContainerCache.PopulateFrom(_imageManagementService.UpdateImage(args.FullPath));
                    break;

                case WatcherChangeTypes.Renamed:
                    var renamedEventArgs = (RenamedEventArgs)args;
                    _imageContainerCache.PopulateFrom(_imageManagementService.RenameImage(renamedEventArgs.OldFullPath, renamedEventArgs.FullPath));
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void Stop()
        {
            folderManagementDisposable?.Dispose();
            folderManagementDisposable = null;
        }

        public IObservable<Unit> ScanFolder(string path)
        {
            return _imageManagementService.ScanFolder(path)
                .Select(container =>
                {
                    _imageContainerCache.AddOrUpdate(container);
                    return Unit.Default;
                })
                .LastAsync()
                .SelectMany(unit => _folderRulesManagementService
                    .AddFolderManagementRule(
                        new FolderRule
                        {
                            Path = path,
                            Action = FolderRuleActionEnum.Once
                        }));
        }
    }
}