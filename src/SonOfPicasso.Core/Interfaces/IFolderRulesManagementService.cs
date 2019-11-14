using System;
using System.Collections.Generic;
using System.Reactive;
using SonOfPicasso.Data.Model;

namespace SonOfPicasso.Core.Interfaces
{
    public interface IFolderRulesManagementService
    {
        IObservable<Unit> ResetFolderManagementRules(IEnumerable<IFolderRuleInput> folderRules);
        IObservable<IList<FolderRule>> GetFolderManagementRules();
        IObservable<Unit> AddFolderManagementRule(FolderRule folderRule);
        IObservable<Unit> ResetFolderManagementRules(IEnumerable<FolderRule> folderRules);
    }
}