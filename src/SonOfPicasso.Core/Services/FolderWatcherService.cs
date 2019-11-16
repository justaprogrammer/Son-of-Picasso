﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Reactive.Linq;
using Serilog;
using SonOfPicasso.Core.Extensions;
using SonOfPicasso.Core.Interfaces;
using SonOfPicasso.Core.Scheduling;
using SonOfPicasso.Data.Model;

namespace SonOfPicasso.Core.Services
{
    public class FolderWatcherService : IFolderWatcherService
    {
        private readonly IFileSystem _fileSystem;
        private readonly ILogger _logger;
        private readonly ISchedulerProvider _schedulerProvider;

        public FolderWatcherService(IFileSystem fileSystem, ILogger logger, ISchedulerProvider schedulerProvider)
        {
            _fileSystem = fileSystem;
            _logger = logger;
            _schedulerProvider = schedulerProvider;
        }

        public IObservable<FileSystemEventArgs> WatchFolders(IEnumerable<FolderRule> folderRules,
            IEnumerable<string> extensionFilters = null)
        {
            var itemsDictionary = folderRules.GetTopLevelItemDictionary();

            var observable = itemsDictionary
                .ToObservable()
                .Select(keyValuePair =>
                {
                    return Observable.Using(
                        () => _fileSystem.FileSystemWatcher.FromPath(keyValuePair.Key),
                        fileSystemWatcher =>
                        {
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

                            var rules = keyValuePair.Value
                                .OrderByDescending(rule => rule.Path.Length)
                                .ToArray();

                            return
                                Observable.Merge(d1, d2, d3, d4)
                                    .Select(args => InRuleSet(args, rules))
                                    .Where(args => args != null);
                        });
                })
                .SelectMany(observables => Observable.Merge(observables));

            if (extensionFilters != null)
            {
                var extensionsHash = extensionFilters.ToHashSet();
                observable = observable
                    .Where(args => extensionsHash.Contains(_fileSystem.Path.GetExtension(args.FullPath)));
            }

            observable = observable
                .Where(args =>
                {
                    if (args.ChangeType == WatcherChangeTypes.Deleted) return true;

                    try
                    {
                        return !_fileSystem.File.GetAttributes(args.FullPath).HasFlag(FileAttributes.Directory);
                    }
                    catch (FileNotFoundException)
                    {
                        return false;
                    }
                });

            return observable;
        }

        private FileSystemEventArgs InRuleSet(FileSystemEventArgs fileSystemEventArgs, IList<FolderRule> folderRules)
        {
            switch (fileSystemEventArgs.ChangeType)
            {
                case WatcherChangeTypes.Created:
                case WatcherChangeTypes.Deleted:
                case WatcherChangeTypes.Changed:
                    foreach (var folderRule in folderRules)
                        if (fileSystemEventArgs.FullPath.StartsWith(folderRule.Path))
                        {
                            if (folderRule.Action == FolderRuleActionEnum.Always) return fileSystemEventArgs;

                            return null;
                        }

                    return fileSystemEventArgs;

                case WatcherChangeTypes.Renamed:
                    return fileSystemEventArgs;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}