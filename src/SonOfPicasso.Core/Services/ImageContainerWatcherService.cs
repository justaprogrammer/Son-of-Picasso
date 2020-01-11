using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using DynamicData;
using Serilog;
using SonOfPicasso.Core.Extensions;
using SonOfPicasso.Core.Interfaces;
using SonOfPicasso.Core.Model;
using SonOfPicasso.Core.Scheduling;

namespace SonOfPicasso.Core.Services
{
    public sealed class ImageContainerWatcherService : IImageContainerWatcherService, IDisposable
    {
        private readonly Subject<string> _fileDeletedSubject = new Subject<string>();
        private readonly Subject<string> _fileDiscoveredSubject = new Subject<string>();

        private readonly IFileSystem _fileSystem;
        private readonly ILogger _logger;

        private readonly ISchedulerProvider _schedulerProvider;

        private CompositeDisposable _watchers;
        private CompositeDisposable _subscriptions;

        public ImageContainerWatcherService(
            ISchedulerProvider schedulerProvider,
            ILogger logger,
            IFileSystem fileSystem)
        {
            _schedulerProvider = schedulerProvider;
            _logger = logger;
            _fileSystem = fileSystem;
        }

        public void Dispose()
        {
            _subscriptions?.Dispose();
            _watchers?.Dispose();
            _fileDiscoveredSubject.Dispose();
            _fileDeletedSubject.Dispose();
        }

        public IObservable<string> FileDiscovered => _fileDiscoveredSubject.AsObservable();

        public IObservable<string> FileDeleted => _fileDeletedSubject.AsObservable();

        public void Start(IObservableCache<ImageRef, string> imageRefCache, IList<string> paths)
        {
            if (imageRefCache == null) throw new ArgumentNullException(nameof(imageRefCache));
            if (paths == null) throw new ArgumentNullException(nameof(paths));

            _subscriptions?.Dispose();
            _watchers?.Dispose();

            _watchers = new CompositeDisposable();
            _subscriptions = new CompositeDisposable();

            foreach (var path in paths)
            {
                var pollingFileSystemWatcher = new PollingFileSystemWatcher(path, "*",
                    new EnumerationOptions
                    {
                        RecurseSubdirectories = true
                    });

                _watchers.Add(pollingFileSystemWatcher);

                var subscription = Observable
                    .FromEventPattern<PollingFileSystemEventHandler, PollingFileSystemEventArgs>(
                        action => pollingFileSystemWatcher.ChangedDetailed += action,
                        action => pollingFileSystemWatcher.ChangedDetailed -= action)
                    .SubscribeOn(_schedulerProvider.TaskPool)
                    .Select(pattern => pattern.EventArgs)
                    .Subscribe(args =>
                    {
                        foreach (var change in args.Changes)
                        {
                            var filePath = _fileSystem.Path.Combine(change.Directory, change.Name);
                            var imageRef = imageRefCache.Lookup(filePath);

                            switch (change.ChangeType)
                            {
                                case WatcherChangeTypes.Created:
                                case WatcherChangeTypes.Changed:

                                    var fileInfo = _fileSystem.FileInfo.FromFileName(filePath);

                                    if (!imageRef.HasValue)
                                    {
                                        _logger.Debug("Discovered {Path}", fileInfo.FullName);
                                        _fileDiscoveredSubject.OnNext(fileInfo.FullName);
                                    }
                                    else if (imageRef.Value.LastWriteTime != fileInfo.LastWriteTimeUtc ||
                                             imageRef.Value.CreationTime != fileInfo.CreationTimeUtc)
                                    {
                                        _logger.Debug("File Updated {Path}", fileInfo.FullName);
                                        _fileDiscoveredSubject.OnNext(fileInfo.FullName);
                                    }

                                    break;

                                case WatcherChangeTypes.Deleted:
                                    if (imageRef.HasValue)
                                    {
                                        _fileDeletedSubject.OnNext(filePath);
                                    }
                                    break;
                            }
                        }
                    });

                _subscriptions.Add(subscription);

                pollingFileSystemWatcher.Start();
            }
        }

        public void Stop()
        {
            _subscriptions?.Dispose();
            _watchers?.Dispose();

            _subscriptions = null;
            _watchers = null;
        }
    }
}