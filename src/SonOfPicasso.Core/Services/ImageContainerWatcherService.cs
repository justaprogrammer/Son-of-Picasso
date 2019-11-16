using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using DynamicData;
using Serilog;
using SonOfPicasso.Core.Extensions;
using SonOfPicasso.Core.Interfaces;
using SonOfPicasso.Core.Model;
using SonOfPicasso.Core.Scheduling;
using SonOfPicasso.Data.Model;

namespace SonOfPicasso.Core.Services
{
    public class ImageContainerWatcherService : IImageContainerWatcherService, IDisposable
    {
        private readonly CompositeDisposable _disposables;
        private readonly IFileSystem _fileSystem;
        private readonly IFolderRulesManagementService _folderRulesManagementService;
        private readonly ILogger _logger;
        private readonly ISchedulerProvider _schedulerProvider;

        private IObservableCache<ImageRef, string> _imageRefCache;

        public ImageContainerWatcherService(ISchedulerProvider schedulerProvider,
            ILogger logger,
            IFolderRulesManagementService folderRulesManagementService,
            IFileSystem fileSystem)
        {
            _schedulerProvider = schedulerProvider;
            _logger = logger;
            _folderRulesManagementService = folderRulesManagementService;
            _fileSystem = fileSystem;
            _disposables = new CompositeDisposable();
        }

        public void Dispose()
        {
            _disposables?.Dispose();
        }

        public IObservable<Unit> Start(IObservableCache<ImageRef, string> imageRefCache)
        {
            if (imageRefCache == null) throw new ArgumentNullException(nameof(imageRefCache));

            return Observable.Defer(() =>
            {
                _logger.Verbose("Start");
                _imageRefCache = imageRefCache;

                return _folderRulesManagementService.GetFolderManagementRules();
            }).SelectMany(SubscribeWatchers);
        }

        private IObservable<Unit> SubscribeWatchers(IList<FolderRule> list)
        {
            _logger.Verbose("SubscribeWatchers");

            var itemsDictionary = list.ToDictionary(rule => rule.Path,
                rule => rule.Action);

            var topLevelItems = list.GetTopLevelItemDictionary();

            var subject = new Subject<Unit>();

            var observable = topLevelItems
                .ToObservable()
                .Select(keyValuePair =>
                {
                    return Observable.Using(() =>
                        {
                            _logger.Verbose("Creating Create/Change FileSystemWatcher {Path}", keyValuePair.Key);
                            return _fileSystem.FileSystemWatcher.FromPath(keyValuePair.Key);
                        },
                        fileSystemWatcher =>
                        {
                            _logger.Verbose("Adding event handlers to watcher");

                            var d1 = Observable.FromEventPattern<FileSystemEventHandler, FileSystemEventArgs>(
                                    action => fileSystemWatcher.Created += action,
                                    action => fileSystemWatcher.Created -= action)
                                .ObserveOn(_schedulerProvider.TaskPool)
                                .Select(pattern => pattern.EventArgs);

                            var d2 = Observable.FromEventPattern<FileSystemEventHandler, FileSystemEventArgs>(
                                    action => fileSystemWatcher.Deleted += action,
                                    action => fileSystemWatcher.Deleted -= action)
                                .ObserveOn(_schedulerProvider.TaskPool)
                                .Select(pattern => pattern.EventArgs);

                            var d3 = Observable.FromEventPattern<FileSystemEventHandler, FileSystemEventArgs>(
                                    action => fileSystemWatcher.Changed += action,
                                    action => fileSystemWatcher.Changed -= action)
                                .ObserveOn(_schedulerProvider.TaskPool)
                                .Select(pattern => pattern.EventArgs);

                            var d4 = Observable.FromEventPattern<RenamedEventHandler, RenamedEventArgs>(
                                    action => fileSystemWatcher.Renamed += action,
                                    action => fileSystemWatcher.Renamed -= action)
                                .ObserveOn(_schedulerProvider.TaskPool)
                                .Select(pattern => (FileSystemEventArgs) pattern.EventArgs);

                            // https://docs.microsoft.com/en-us/dotnet/api/system.io.filesystemwatcher.internalbuffersize?view=netframework-4.8
                            // Default is 8k, Max is 64k, for best performance use multiples of 4k
                            fileSystemWatcher.InternalBufferSize = 4096 * 8;

                            fileSystemWatcher.IncludeSubdirectories = true;
                            fileSystemWatcher.EnableRaisingEvents = true;

                            subject.OnNext(Unit.Default);

                            return Observable.Merge(d1, d2, d3, d4);
                        });
                })
                .SelectMany(o => Observable.Merge(o))
                .Publish();

            observable.Subscribe(args =>
                {
                    if (args.ChangeType != WatcherChangeTypes.Deleted)
                        if (_fileSystem.Directory.Exists(args.FullPath))
                        {
                            _logger.Verbose("Directory Event {Name} {FullPath} {ChangeType}", args.Name, args.FullPath,
                                args.ChangeType);
                            return;
                        }

                    _logger.Verbose("File Event {Name} {FullPath} {ChangeType}", args.Name, args.FullPath,
                        args.ChangeType);
                })
                .DisposeWith(_disposables);

            observable
                .Where(args =>
                    args.ChangeType == WatcherChangeTypes.Created || args.ChangeType == WatcherChangeTypes.Changed)
                .Select(args => args.FullPath.Substring(0, args.FullPath.Length - args.Name.Length))
                .GroupByUntil(
                    args => args,
                    args => Observable.Timer(TimeSpan.FromSeconds(1), _schedulerProvider.TaskPool))
                .SelectMany(groupedObservable =>
                    groupedObservable.Select((path, count) => (path, count))
                        .LastAsync())
                .Subscribe(tuple =>
                {
                    var (path, count) = tuple;
                    _logger.Verbose("Grouped Scanner Event {Path} {GroupCount}", path, count);
                })
                .DisposeWith(_disposables);

            observable
                .Where(args => args.ChangeType == WatcherChangeTypes.Deleted)
                .Select(args => args.FullPath)
                .Subscribe(path =>
                {
                    _logger.Verbose("Delete Event {Path}", path);
                })
                .DisposeWith(_disposables);

            observable
                .Where(args => args.ChangeType == WatcherChangeTypes.Renamed)
                .Cast<RenamedEventArgs>()
                .Select<RenamedEventArgs, (string oldFullPath, string fullPath)>(args =>
                    (args.OldFullPath, args.FullPath))
                .Subscribe(tuple =>
                {
                    var (oldFullPath, fullPath) = tuple;
                    _logger.Verbose("Renamed Event {OldPath} {Path}", oldFullPath, fullPath);
                })
                .DisposeWith(_disposables);

            observable.Connect()
                .DisposeWith(_disposables);

            return subject.AsObservable()
                .Do(unit => { ; })
                .Skip(topLevelItems.Count - 1)
                .Do(unit => { ; })
                .Replay(1);
        }
    }
}