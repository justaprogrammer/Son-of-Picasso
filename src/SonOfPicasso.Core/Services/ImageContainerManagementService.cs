using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData;
using Serilog;
using SonOfPicasso.Core.Extensions;
using SonOfPicasso.Core.Interfaces;
using SonOfPicasso.Core.Model;
using SonOfPicasso.Core.Scheduling;
using SonOfPicasso.Data.Model;
using Svg;

namespace SonOfPicasso.Core.Services
{
    public class ImageContainerManagementService : IImageContainerManagementService
    {
        private readonly CompositeDisposable _disposables;
        private readonly IObservableCache<ImageRef, string> _folderImageRefCache;
        private readonly IFolderRulesManagementService _folderRulesManagementService;
        private readonly SourceCache<IImageContainer, string> _imageContainerCache;
        private readonly IImageContainerOperationService _imageContainerOperationService;
        private readonly IImageContainerWatcherService _imageContainerWatcherService;
        private readonly ILogger _logger;
        private readonly ISchedulerProvider _schedulerProvider;

        public ImageContainerManagementService(
            IImageContainerOperationService imageContainerOperationService,
            IImageContainerWatcherService imageContainerWatcherService,
            IFolderRulesManagementService folderRulesManagementService,
            ISchedulerProvider schedulerProvider,
            ILogger logger)
        {
            _disposables = new CompositeDisposable();
            _imageContainerOperationService = imageContainerOperationService;
            _imageContainerWatcherService = imageContainerWatcherService;
            _folderRulesManagementService = folderRulesManagementService;
            _schedulerProvider = schedulerProvider;
            _logger = logger;
            _imageContainerCache = new SourceCache<IImageContainer, string>(imageContainer => imageContainer.Key);
            _folderImageRefCache = _imageContainerCache
                .Connect()
                .ObserveOn(schedulerProvider.TaskPool)
                .Filter(container => container.ContainerType == ImageContainerTypeEnum.Folder)
                .TransformMany(container => container.ImageRefs, imageRef => imageRef.ImagePath)
                .AsObservableCache();

            _imageContainerWatcherService.FileDiscovered
                .SelectMany(path => _imageContainerOperationService.ScanImage(path))
                .Subscribe()
                .DisposeWith(_disposables);

            _imageContainerWatcherService.FileDeleted
                .SelectMany(path => _imageContainerOperationService.DeleteImage(path))
                .Subscribe(container => _imageContainerCache.AddOrUpdate(container))
                .DisposeWith(_disposables);

            _imageContainerWatcherService.FileRenamed
                .SelectMany(tuple => imageContainerOperationService.RenameImage(tuple.oldFullPath, tuple.fullPath))
                .Subscribe(container => _imageContainerCache.AddOrUpdate(container))
                .DisposeWith(_disposables);
            
            imageContainerOperationService.ScanImageObservable
                .Select((imageRef) => imageRef.ContainerId)
                .Buffer(TimeSpan.FromSeconds(2))
                .SelectMany(observable => observable.Distinct())
                .SelectMany(i => _imageContainerOperationService.GetFolderImageContainer(i))
                .Subscribe(container => _imageContainerCache.AddOrUpdate(container))
                .DisposeWith(_disposables);
        }

        public IConnectableCache<IImageContainer, string> ImageContainerCache => _imageContainerCache;

        public IConnectableCache<ImageRef, string> FolderImageRefCache => _folderImageRefCache;

        public IObservable<ImageRef> ScanImageObservable => _imageContainerOperationService.ScanImageObservable;

        public void Dispose()
        {
            _imageContainerCache?.Dispose();
            _folderImageRefCache?.Dispose();
            _disposables?.Dispose();
        }

        public IObservable<Unit> ResetRules(IEnumerable<FolderRule> folderRules)
        {
            return Observable.DeferAsync(async token =>
            {
                _logger.Verbose("ResetRules");

                var folderRulesArray = folderRules.ToArray();

                var paths = folderRulesArray
                    .GetTopLevelItemDictionary()
                    .Keys
                    .ToArray();

                await _folderRulesManagementService.ResetFolderManagementRules(folderRulesArray);

                var changes = await _imageContainerOperationService.ApplyRuleChanges(folderRulesArray);
                
                _imageContainerCache.Remove(changes.DeletedFolderIds.Select(FolderImageContainer.GetContainerKey));

                _imageContainerWatcherService.Stop();

                _imageContainerWatcherService.Start(_folderImageRefCache, paths);

                return Observable.Return(Unit.Default);
            });
        }

        public IObservable<ResetChanges> PreviewResetRulesChanges(IEnumerable<FolderRule> folderRules)
        {
            return _imageContainerOperationService.PreviewRuleChangesEffect(folderRules);
        }

        public IObservable<Unit> Start()
        {
            return Observable.DeferAsync(async token =>
            {
                _logger.Debug("Starting");

                var imageContainers = await _imageContainerOperationService
                    .GetAllImageContainers()
                    .ToArray();

                foreach (var imageContainer in imageContainers)
                {
                    _imageContainerCache.AddOrUpdate(imageContainer);
                }

                var folderRules = await _folderRulesManagementService.GetFolderManagementRules();

                var folderRuleDictionary = folderRules.ToDictionary(rule => rule.Path, rule => rule.Action);

                var topLevelItemDictionary = folderRules.GetTopLevelItemDictionary();

                _imageContainerWatcherService.Start(_folderImageRefCache, topLevelItemDictionary.Keys.ToArray());

                foreach (var (key, _) in topLevelItemDictionary)
                {
                    if (folderRuleDictionary[key] == FolderRuleActionEnum.Always)
                    {
                        ScanFolder(key)
                            .Subscribe();
                    }
                }

                _logger.Debug("Started");

                return Observable.Return(Unit.Default);
            });
        }

        public void Stop()
        {
            _imageContainerWatcherService.Stop();
        }

        public IObservable<Unit> ScanFolder(string path)
        {
            return _folderRulesManagementService
                .AddFolderManagementRule(
                    new FolderRule
                    {
                        Path = path,
                        Action = FolderRuleActionEnum.Once
                    })
                .SelectMany(unit =>_imageContainerOperationService.ScanFolder(path, _folderImageRefCache));
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
                .Do(container => _imageContainerCache.Remove(AlbumImageContainer.GetContainerKey(albumId)));
        }
    }
}