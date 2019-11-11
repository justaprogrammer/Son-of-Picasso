using System.Collections.Generic;
using DynamicData.Binding;
using ReactiveUI;
using SonOfPicasso.Core.Interfaces;
using SonOfPicasso.UI.ViewModels;

namespace SonOfPicasso.UI.Interfaces
{
    public interface IManageFolderRulesViewModel: IReactiveObject
    {
        IList<IFolderRuleInput> Folders { get; }
    }
}