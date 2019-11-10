using System;
using System.Collections.Generic;
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
using SonOfPicasso.UI.ViewModels.Abstract;

namespace SonOfPicasso.UI.ViewModels
{
    public class FolderRuleViewModel : ViewModelBase, IFolderRuleInput, IDisposable
    {
        private readonly ISchedulerProvider _schedulerProvider;
        private readonly IDirectoryInfoPermissionsService _directoryInfoPermissionsService;
        private readonly Func<FolderRuleViewModel> _folderViewModelFactory;

        public enum ShouldShowChildReasonEnum
        {
            ShouldNot,
            ChildInRules
        }

        private readonly ObservableCollectionExtended<FolderRuleViewModel> _children;
        private readonly SourceCache<FolderRuleViewModel, string> _manageFolderViewModelsSourceCache;

        private IReadOnlyDictionary<string, FolderRuleActionEnum> _currentRules;
        private IDirectoryInfo _directoryInfo;
        private FolderRuleActionEnum _folderRuleAction;
        private IManageFolderRulesViewModel _manageFolderRulesViewModel;
        private ObservableAsPropertyHelper<bool> _shouldShowPropertyHelper;
        private ShouldShowChildReasonEnum _shouldShowReason = ShouldShowChildReasonEnum.ShouldNot;

        public FolderRuleViewModel(
            ISchedulerProvider schedulerProvider,
            IDirectoryInfoPermissionsService directoryInfoPermissionsService,
            Func<FolderRuleViewModel> folderViewModelFactory,
            ViewModelActivator activator) : base(activator)
        {
            _schedulerProvider = schedulerProvider;
            _directoryInfoPermissionsService = directoryInfoPermissionsService;
            _folderViewModelFactory = folderViewModelFactory;
            _manageFolderViewModelsSourceCache = new SourceCache<FolderRuleViewModel, string>(model => model.Path);

            _children = new ObservableCollectionExtended<FolderRuleViewModel>();

            _shouldShowPropertyHelper = this.WhenPropertyChanged(model => model.ShouldShowReason)
                .Select(propertyValue => propertyValue.Value != ShouldShowChildReasonEnum.ShouldNot)
                .ToProperty(this, model => model.ShouldShow);

            this.WhenActivated(disposable =>
            {
                Observable.Defer(() =>
                    {
                        return _directoryInfo
                            .EnumerateDirectories()
                            .ToObservable()
                            .Where(_directoryInfoPermissionsService.IsReadable)
                            .Select(info => CreateFolderViewModel(info));
                    })
                    .SubscribeOn(_schedulerProvider.TaskPool)
                    .Subscribe(folderRuleViewModel =>
                    {
                        _manageFolderViewModelsSourceCache.AddOrUpdate(folderRuleViewModel);
                    });

                _manageFolderViewModelsSourceCache
                    .Connect()
                    .ObserveOn(schedulerProvider.TaskPool)
                    .Filter(_manageFolderRulesViewModel
                        .WhenPropertyChanged(model => model.HideUnselected)
                        .ObserveOn(schedulerProvider.TaskPool)
                        .Select(value => value.Value)
                        .Select(hideUnselected =>
                        {
                            return (Func<FolderRuleViewModel, bool>) (model =>
                                hideUnselected == false || model.ShouldShow);
                        }))
                    .ObserveOn(schedulerProvider.MainThreadScheduler)
                    .Bind(_children)
                    .Subscribe();

                this.WhenAny(model => model.FolderRuleAction, change => change.Value)
                    .ObserveOn(schedulerProvider.MainThreadScheduler)
                    .Subscribe(newState =>
                    {
                        foreach (var viewModel in Children) viewModel.FolderRuleAction = newState;
                    })
                    .DisposeWith(disposable);
            });
        }

        private FolderRuleViewModel CreateFolderViewModel(IDirectoryInfo info)
        {
            var folderViewModel = _folderViewModelFactory();
            folderViewModel.Initialize(_manageFolderRulesViewModel, info, _currentRules);
            return folderViewModel;
        }

        public string Name => _directoryInfo.Name;

        public IObservableCollection<FolderRuleViewModel> Children => _children;

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
        }

        public FolderRuleActionEnum FolderRuleAction
        {
            get => _folderRuleAction;
            set => this.RaiseAndSetIfChanged(ref _folderRuleAction, value);
        }

        public string Path => _directoryInfo.FullName;

        IList<IFolderRuleInput> IFolderRuleInput.Children => _children.Cast<IFolderRuleInput>().ToArray();

        public void Initialize(IManageFolderRulesViewModel manageFolderRulesViewModel, IDirectoryInfo directoryInfo,
            IReadOnlyDictionary<string, FolderRuleActionEnum> currentRules)
        {
            _directoryInfo = directoryInfo;
            _manageFolderRulesViewModel = manageFolderRulesViewModel;

            if (currentRules.TryGetValue(_directoryInfo.FullName, out var folderRuleAction))
                _folderRuleAction = folderRuleAction;
            else
                _folderRuleAction = FolderRuleActionEnum.Remove;

            var anyChildHasRule = currentRules.Any(s => s.Value != FolderRuleActionEnum.Remove
                                                        && s.Key.StartsWith(directoryInfo.FullName));

            if (anyChildHasRule) ShouldShowReason = ShouldShowChildReasonEnum.ChildInRules;

            _currentRules = currentRules;
        }
    }
}