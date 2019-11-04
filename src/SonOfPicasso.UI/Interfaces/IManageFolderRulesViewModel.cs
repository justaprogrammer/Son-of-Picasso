using DynamicData.Binding;
using SonOfPicasso.UI.ViewModels;

namespace SonOfPicasso.UI.Interfaces
{
    public interface IManageFolderRulesViewModel
    {
        IObservableCollection<FolderRuleViewModel> Folders { get; }
    }
}