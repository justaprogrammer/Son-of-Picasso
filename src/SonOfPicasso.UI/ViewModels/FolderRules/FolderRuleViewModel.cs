using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO.Abstractions;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using SonOfPicasso.Core.Interfaces;
using SonOfPicasso.Core.Scheduling;
using SonOfPicasso.Data.Model;
using SonOfPicasso.UI.WPF.Interfaces;

namespace SonOfPicasso.UI.WPF.ViewModels.FolderRules
{
    public class FolderRuleViewModel : ReactiveObject, IFolderRuleInput, IDisposable
    {
        private readonly ReadOnlyObservableCollection<FolderRuleViewModel> _children;
        private readonly ReadOnlyObservableCollection<FolderRuleViewModel> _visibleChildren;
        private readonly ISourceCache<FolderRuleViewModel, string> _childrenSourceCache;

        private readonly CompositeDisposable _disposables;
        private FolderRuleActionEnum _folderRuleAction;
        private bool _isLoaded;
        private bool _isParentOfRule;
        private bool _isActiveRule;

        public FolderRuleViewModel(IDirectoryInfo directoryInfo, IManageFolderRulesViewModel manageFolderRulesViewModel, ISchedulerProvider schedulerProvider)
        {
            DirectoryInfo = directoryInfo;
            
            _disposables = new CompositeDisposable();

            _childrenSourceCache = new SourceCache<FolderRuleViewModel, string>(model => model.Path);

            var childrenSourceCacheChanges = _childrenSourceCache
                .Connect()
                .Publish();

            childrenSourceCacheChanges
                .Filter(manageFolderRulesViewModel
                    .WhenPropertyChanged(model => model.HideUnselected)
                    .Select(propertyValue => propertyValue.Value)
                    .Select(hideUnselected => (Func<FolderRuleViewModel, bool>) (model =>
                        !hideUnselected || model.IsActiveRule || model.IsParentOfRule)))
                .Sort(Comparer<FolderRuleViewModel>.Create((model1, model2) =>
                    string.CompareOrdinal(model1.Name, model2.Name)))
                .SubscribeOn(schedulerProvider.MainThreadScheduler)
                .Bind(out _visibleChildren)
                .Subscribe()
                .DisposeWith(_disposables);

            childrenSourceCacheChanges
                .SubscribeOn(schedulerProvider.MainThreadScheduler)
                .Bind(out _children)
                .Subscribe()
                .DisposeWith(_disposables);

            childrenSourceCacheChanges
                .Connect()
                .DisposeWith(_disposables);
        }

        public IDirectoryInfo DirectoryInfo { get; }

        public string Name => DirectoryInfo.Name;

        public bool IsLoaded
        {
            get => _isLoaded;
            set => this.RaiseAndSetIfChanged(ref _isLoaded, value);
        }

        public ReadOnlyObservableCollection<FolderRuleViewModel> Children => _children;
        public ReadOnlyObservableCollection<FolderRuleViewModel> VisibleChildren => _visibleChildren;

        public bool IsParentOfRule
        {
            get => _isParentOfRule;
            set => this.RaiseAndSetIfChanged(ref _isParentOfRule, value);
        }

        public bool IsActiveRule
        {
            get => _isActiveRule;
            set => this.RaiseAndSetIfChanged(ref _isActiveRule, value);
        }

        public void Dispose()
        {
            _disposables?.Dispose();
        }

        public string Path => DirectoryInfo.FullName;

        public FolderRuleActionEnum FolderRuleAction
        {
            get => _folderRuleAction;
            set => this.RaiseAndSetIfChanged(ref _folderRuleAction, value);
        }

        IList<IFolderRuleInput> IFolderRuleInput.Children => _children.Cast<IFolderRuleInput>().ToArray();

        public void AddChild(FolderRuleViewModel folderRuleViewModel)
        {
            _childrenSourceCache.AddOrUpdate(folderRuleViewModel);
        }
    }
}