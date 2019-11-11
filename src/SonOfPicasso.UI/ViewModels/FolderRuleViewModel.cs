using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO.Abstractions;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using SonOfPicasso.Core.Interfaces;
using SonOfPicasso.Core.Scheduling;
using SonOfPicasso.Data.Model;
using SonOfPicasso.UI.Interfaces;
using SonOfPicasso.UI.ViewModels.Abstract;

namespace SonOfPicasso.UI.ViewModels
{
    public class FolderRuleViewModel : ViewModelBase, IFolderRuleInput, IDisposable
    {
        public enum ShouldShowChildReasonEnum
        {
            ShouldNot,
            ChildInRules
        }

        private readonly CompositeDisposable _disposables;
        private readonly Func<FolderRuleViewModel> _folderViewModelFactory;
        private readonly object _loadingLock = new object();
        private readonly SourceCache<FolderRuleViewModel, string> _manageFolderViewModelsSourceCache;
        private readonly ISchedulerProvider _schedulerProvider;

        private ReadOnlyObservableCollection<FolderRuleViewModel> _children;
        private IReadOnlyDictionary<string, FolderRuleActionEnum> _currentRules;
        private IDirectoryInfo _directoryInfo;
        private FolderRuleActionEnum _folderRuleAction;
        private bool _loadCalled;
        private IManageFolderRulesViewModel _manageFolderRulesViewModel;

        private ObservableAsPropertyHelper<bool> _shouldShowPropertyHelper;
        private ShouldShowChildReasonEnum _shouldShowReason = ShouldShowChildReasonEnum.ShouldNot;
        private ISubject<FolderRuleViewModel> _onChangedSubject;

        public FolderRuleViewModel(
            ISchedulerProvider schedulerProvider,
            Func<FolderRuleViewModel> folderViewModelFactory,
            ViewModelActivator activator) : base(activator)
        {
            _schedulerProvider = schedulerProvider;
            _folderViewModelFactory = folderViewModelFactory;
            _manageFolderViewModelsSourceCache = new SourceCache<FolderRuleViewModel, string>(model => model.Path);

            _disposables = new CompositeDisposable();

            this.WhenAny(model => model.FolderRuleAction, change => change.Value)
                .ObserveOn(_schedulerProvider.MainThreadScheduler)
                .Subscribe(newState =>
                {
                    if (Children != null)
                        foreach (var viewModel in Children)
                            viewModel.FolderRuleAction = newState;
                })
                .DisposeWith(_disposables);

            this.WhenActivated((CompositeDisposable disposable) =>
            {
                Load();
            });
        }

        public string Name => _directoryInfo.Name;

        public ReadOnlyObservableCollection<FolderRuleViewModel> Children => _children;

        public bool ShouldShow => _shouldShowPropertyHelper?.Value ?? false;

        public ShouldShowChildReasonEnum ShouldShowReason
        {
            get => _shouldShowReason;
            set => this.RaiseAndSetIfChanged(ref _shouldShowReason, value);
        }

        public void Dispose()
        {
            _manageFolderViewModelsSourceCache?.Dispose();
            _shouldShowPropertyHelper?.Dispose();
            _disposables?.Dispose();
        }

        public FolderRuleActionEnum FolderRuleAction
        {
            get => _folderRuleAction;
            set => this.RaiseAndSetIfChanged(ref _folderRuleAction, value);
        }

        public string Path => _directoryInfo.FullName;

        IList<IFolderRuleInput> IFolderRuleInput.Children => _children.Cast<IFolderRuleInput>().ToArray();

        private FolderRuleViewModel CreateFolderViewModel(IDirectoryInfo info)
        {
            var folderViewModel = _folderViewModelFactory();
            folderViewModel.Initialize(_manageFolderRulesViewModel, info, _currentRules, _folderRuleAction,_onChangedSubject );
            return folderViewModel;
        }

        public void Initialize(IManageFolderRulesViewModel manageFolderRulesViewModel, IDirectoryInfo directoryInfo,
            IReadOnlyDictionary<string, FolderRuleActionEnum> currentRules,
            FolderRuleActionEnum inheritedFolderRuleAction,
            ISubject<FolderRuleViewModel> onChangedSubject)
        {
            _directoryInfo = directoryInfo;
            _manageFolderRulesViewModel = manageFolderRulesViewModel;
            _currentRules = currentRules;
            _onChangedSubject = onChangedSubject;

            var sourceCacheConnection = _manageFolderViewModelsSourceCache
                .Connect()
                .Publish();

            sourceCacheConnection
                .Filter(_manageFolderRulesViewModel
                    .WhenPropertyChanged(model => model.HideUnselected)
                    .ObserveOn(_schedulerProvider.TaskPool)
                    .Select(value => value.Value)
                    .Select(hideUnselected =>
                    {
                        return (Func<FolderRuleViewModel, bool>) (model =>
                            hideUnselected == false || model.ShouldShow);
                    }))
                .ObserveOn(_schedulerProvider.MainThreadScheduler)
                .Bind(out _children)
                .Subscribe()
                .DisposeWith(_disposables);

            sourceCacheConnection.Connect();

            _folderRuleAction =
                currentRules.TryGetValue(_directoryInfo.FullName, out var existingFolderRuleAction)
                    ? existingFolderRuleAction
                    : inheritedFolderRuleAction;

            var anyChildHasRule = currentRules.Any(s => s.Value != FolderRuleActionEnum.Remove
                                                        && s.Key.StartsWith(directoryInfo.FullName));

            var shouldLoad = false;
            if (anyChildHasRule)
            {
                _shouldShowReason = ShouldShowChildReasonEnum.ChildInRules;
                shouldLoad = true;
            }

            _shouldShowPropertyHelper = this.WhenPropertyChanged(model => model.ShouldShowReason)
                .Select(propertyValue => propertyValue.Value != ShouldShowChildReasonEnum.ShouldNot)
                .ToProperty(this, model => model.ShouldShow);

            this.WhenPropertyChanged(model => model.FolderRuleAction, false)
                .Select(propertyValue => propertyValue.Sender)
                .Subscribe(_onChangedSubject.OnNext)
                .DisposeWith(_disposables);

            if (shouldLoad) Load();
        }

        private void Load()
        {
            if (!_loadCalled)
                lock (_loadingLock)
                {
                    if (!_loadCalled)
                    {
                        _loadCalled = true;

                        var observable = _manageFolderRulesViewModel.GetAccesibleChildDirectories(_directoryInfo)
                            .SelectMany(directoryInfos => directoryInfos)
                            .Select(CreateFolderViewModel);

                        _manageFolderViewModelsSourceCache.PopulateFrom(observable);
                    }
                }
        }
    }
}