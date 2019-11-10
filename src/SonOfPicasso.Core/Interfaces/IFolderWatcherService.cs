using System;
using System.Collections.Generic;
using System.IO;
using SonOfPicasso.Data.Model;

namespace SonOfPicasso.Core.Interfaces
{
    public interface IFolderWatcherService
    {
        IObservable<FileSystemEventArgs> WatchFolders(IEnumerable<FolderRule> folderRules, IEnumerable<string> extensionFilters = null);
    }
}