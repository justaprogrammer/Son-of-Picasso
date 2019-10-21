using System;
using System.Collections.Generic;
using System.Reactive;
using SonOfPicasso.Data.Model;

namespace SonOfPicasso.Core.Interfaces
{
    public interface IFolderRulesManagementService
    {
        IObservable<Unit> ResetFolderManagementRules(IEnumerable<FolderRule> folderRules);
        IObservable<IList<FolderRule>> GetFolderManagementRules();
    }
}