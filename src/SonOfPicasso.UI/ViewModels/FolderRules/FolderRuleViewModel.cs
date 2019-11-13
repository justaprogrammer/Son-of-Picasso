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
using SonOfPicasso.UI.Interfaces;

namespace SonOfPicasso.UI.ViewModels.FolderRules
{
    public class FolderRuleViewModel : ReactiveObject, IFolderRuleInput, IDisposable
    {
        private readonly ReadOnlyObservableCollection<FolderRuleViewModel> _children;
        private readonly ISourceCache<FolderRuleViewModel, string> _childrenSourceCache;
        private readonly ObservableAsPropertyHelper<bool> _containsCurrentRulePropertyHelper;
        private readonly CompositeDisposable _disposables;
        private readonly ObservableAsPropertyHelper<bool> _isCurrentRulePropertyHelper;

        private FolderRuleActionEnum _folderRuleAction;
        private bool _isLoaded;

        public FolderRuleViewModel(IManageFolderRulesViewModel manageFolderRulesViewModel,
            IDirectoryInfo directoryInfo,
            IDictionary<string, FolderRuleActionEnum> folderRules,
            FolderRuleViewModel parent,
            ISchedulerProvider schedulerProvider)
        {
            Parent = parent;
            DirectoryInfo = directoryInfo;

            if (folderRules.TryGetValue(directoryInfo.FullName, out var folderRuleAction))
                FolderRuleAction = folderRuleAction;
            else
                FolderRuleAction = parent?.FolderRuleAction ?? FolderRuleActionEnum.Remove;

            _disposables = new CompositeDisposable();

            _childrenSourceCache = new SourceCache<FolderRuleViewModel, string>(model => model.Path);

            _childrenSourceCache
                .Connect()
                .Filter(manageFolderRulesViewModel
                    .WhenPropertyChanged(model => model.HideUnselected)
                    .Select(propertyValue => propertyValue.Value)
                    .Select(hideUnselected => (Func<FolderRuleViewModel, bool>) (model =>
                        !hideUnselected || model.IsCurrentRule || model.ContainsFolderWithRule)))
                .Sort(Comparer<FolderRuleViewModel>.Create((model1, model2) =>
                    string.CompareOrdinal(model1.Name, model2.Name)))
                .SubscribeOn(schedulerProvider.MainThreadScheduler)
                .Bind(out _children)
                .Subscribe()
                .DisposeWith(_disposables);

            var currentFolderRuleStatus = manageFolderRulesViewModel
                .CurrentFolderManagementRules
                .Select(currentFolderRules =>
                {
                    var isRule = currentFolderRules.ContainsKey(directoryInfo.FullName);

                    var containsRule = !isRule &&
                                       currentFolderRules
                                           .Any(s => s.Value != FolderRuleActionEnum.Remove
                                                     && s.Key.Length > directoryInfo.FullName.Length
                                                     && s.Key.StartsWith(directoryInfo.FullName));

                    return (isRule, containsRule);
                })
                .Publish();

            _isCurrentRulePropertyHelper = currentFolderRuleStatus
                .Select(tuple => tuple.isRule)
                .ToProperty(this, model => model.IsCurrentRule);

            _containsCurrentRulePropertyHelper = currentFolderRuleStatus
                .Select(tuple => tuple.containsRule)
                .ToProperty(this, model => model.IsCurrentRule);

            currentFolderRuleStatus
                .Connect()
                .DisposeWith(_disposables);

            this.WhenPropertyChanged(model => model.FolderRuleAction, false)
                .Subscribe(propertyValue =>
                {
                    foreach (var customFolderRuleInput in Children)
                        customFolderRuleInput.FolderRuleAction = propertyValue.Value;
                })
                .DisposeWith(_disposables);
        }

        public FolderRuleViewModel Parent { get; }

        public bool IsCurrentRule => _isCurrentRulePropertyHelper.Value;

        public IDirectoryInfo DirectoryInfo { get; }

        public string Name => DirectoryInfo.Name;

        public bool IsLoaded
        {
            get => _isLoaded;
            set => this.RaiseAndSetIfChanged(ref _isLoaded, value);
        }

        public ReadOnlyObservableCollection<FolderRuleViewModel> Children => _children;

        public bool ContainsFolderWithRule => _containsCurrentRulePropertyHelper.Value;

        public void Dispose()
        {
            _childrenSourceCache?.Dispose();
            _disposables?.Dispose();
            _containsCurrentRulePropertyHelper?.Dispose();
            _isCurrentRulePropertyHelper?.Dispose();
        }

        public string Path => DirectoryInfo.FullName;

        public FolderRuleActionEnum FolderRuleAction
        {
            get => _folderRuleAction;
            set => this.RaiseAndSetIfChanged(ref _folderRuleAction, value);
        }

        IList<IFolderRuleInput> IFolderRuleInput.Children => Children.Cast<IFolderRuleInput>().ToArray();

        public void AddChild(FolderRuleViewModel folderRuleViewModel)
        {
            _childrenSourceCache.AddOrUpdate(folderRuleViewModel);
        }
    }
}