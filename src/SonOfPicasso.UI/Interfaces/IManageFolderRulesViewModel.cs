using System.ComponentModel;
using DynamicData.Binding;
using ReactiveUI;
using SonOfPicasso.UI.ViewModels;

namespace SonOfPicasso.UI.Interfaces
{
    public interface IManageFolderRulesViewModel: IReactiveObject
    {
        IObservableCollection<FolderRuleViewModel> Folders { get; }
        bool HideUnselected { get; set; }
    }
}