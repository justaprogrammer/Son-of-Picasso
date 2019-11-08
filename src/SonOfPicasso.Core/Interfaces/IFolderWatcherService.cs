using System;
using System.Collections.Generic;
using System.IO;
using SonOfPicasso.Data.Model;

namespace SonOfPicasso.Core.Services
{
    public interface IFolderWatcherService
    {
        IObservable<FileSystemEventArgs> WatchFolders(IEnumerable<FolderRule> folderRules);
    }
}