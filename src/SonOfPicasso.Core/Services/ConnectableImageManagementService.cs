using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    public class ConnectableImageManagementService : IConnectableManagementService, IDisposable
    {
        private readonly IFolderRulesManagementService _folderRulesManagementService;
        private readonly ISchedulerProvider _schedulerProvider;
        private readonly ILogger _logger;
        private readonly IFolderWatcherService _folderWatcherService;

        private readonly SourceCache<IImageContainer, string> _imageContainerCache;
        private readonly IImageManagementService _imageManagementService;
        private readonly IObservableCache<ImageRef, string> _imageRefCache;
        private IDisposable _folderManagementDisposable;

        public ConnectableImageManagementService(IImageManagementService imageManagementService,
            IFolderWatcherService folderWatcherService,
            IFolderRulesManagementService folderRulesManagementService,
            ISchedulerProvider schedulerProvider,
            ILogger logger)
        {
            _imageManagementService = imageManagementService;
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
            return _imageManagementService.GetAllImageContainers()
                .Do(container => _imageContainerCache.AddOrUpdate(container))
                .LastOrDefaultAsync()
                .Select(_ => Unit.Default)
                .Do(_ => StartWatcher());
        }

        private void StartWatcher()
        {
            _folderManagementDisposable = _folderRulesManagementService.GetFolderManagementRules()
                .SelectMany(list => _folderWatcherService.WatchFolders(list))
                .Buffer(TimeSpan.FromSeconds(1), _schedulerProvider.TaskPool)
                .SelectMany(list =>
                {
                    var hashSet = new HashSet<string>();
                    return list.Where(args =>
                    {
                        if (args.ChangeType == WatcherChangeTypes.Created)
                        {
                            hashSet.Add(args.FullPath);
                            return true;
                        }

                        if (args.ChangeType == WatcherChangeTypes.Changed)
                        {
                            return !hashSet.Contains(args.FullPath);
                        }

                        return true;
                    });
                })
                .Subscribe(HandlerFolderWatcherEvent);
        }

        private void HandlerFolderWatcherEvent(FileSystemEventArgs args)
        {
            _logger.Debug("HandlerFolderWatcherEvent {ChangeType} {Name}", args.ChangeType, args.Name);

            switch (args.ChangeType)
            {
                case WatcherChangeTypes.Created:
                case WatcherChangeTypes.Changed:
                    _imageContainerCache.PopulateFrom(_imageManagementService.AddOrUpdateImage(args.FullPath));
                    break;

                case WatcherChangeTypes.Deleted:
                    _imageContainerCache.PopulateFrom(_imageManagementService.DeleteImage(args.FullPath));
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
            _folderManagementDisposable?.Dispose();
            _folderManagementDisposable = null;
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