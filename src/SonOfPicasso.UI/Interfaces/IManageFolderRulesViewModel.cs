using System;
using System.ComponentModel;
using System.IO.Abstractions;
using DynamicData.Binding;
using ReactiveUI;
using SonOfPicasso.UI.ViewModels;

namespace SonOfPicasso.UI.Interfaces
{
    public interface IManageFolderRulesViewModel: IReactiveObject
    {
        IObservableCollection<FolderRuleViewModel> Folders { get; }
        bool HideUnselected { get; set; }
        IObservable<IDirectoryInfo[]> GetAccesibleChildDirectories(IDirectoryInfo directoryInfo);
    }
}