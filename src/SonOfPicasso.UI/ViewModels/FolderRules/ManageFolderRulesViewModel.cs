using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using SonOfPicasso.Core.Interfaces;
using SonOfPicasso.Core.Scheduling;
using SonOfPicasso.Core.Services;
using SonOfPicasso.Data.Model;
using SonOfPicasso.UI.Interfaces;
using SonOfPicasso.UI.ViewModels.Abstract;

namespace SonOfPicasso.UI.ViewModels.FolderRules
{
    public class ManageFolderRulesViewModel : ViewModelBase, IManageFolderRulesViewModel, IDisposable
    {
        private readonly IObservable<IDictionary<string, FolderRuleActionEnum>> _currentFolderManagementRules;
        private readonly IDirectoryInfoPermissionsService _directoryInfoPermissionsService;
        private readonly CompositeDisposable _disposables;
        private readonly IDriveInfoFactory _driveInfoFactory;
        private readonly IFolderRulesManagementService _folderRulesManagementService;
        private readonly ConcurrentDictionary<string, FolderRuleViewModel> _folderRuleViewModelDictionary;
        private readonly ObservableCollectionExtended<FolderRuleViewModel> _foldersObservableCollection;
        private readonly ISchedulerProvider _schedulerProvider;
        private readonly ReadOnlyObservableCollection<FolderRule> _watchedPaths;

        private readonly SourceCache<FolderRule, string> _watchedPathsSourceCache;

        private bool _hideUnselected;
        private FolderRuleViewModel _selectedItem;

        public ManageFolderRulesViewModel(ViewModelActivator activator,
            IDriveInfoFactory driveInfoFactory,
            IDirectoryInfoPermissionsService directoryInfoPermissionsService,
            IFolderRulesManagementService folderRulesManagementService,
            ISchedulerProvider schedulerProvider) : base(activator)
        {
            _driveInfoFactory = driveInfoFactory;
            _directoryInfoPermissionsService = directoryInfoPermissionsService;
            _folderRulesManagementService = folderRulesManagementService;
            _schedulerProvider = schedulerProvider;
            _disposables = new CompositeDisposable();

            _watchedPathsSourceCache = new SourceCache<FolderRule, string>(model => model.Path);

            _watchedPathsSourceCache
                .Connect()
                .Sort(Comparer<FolderRule>.Create((model1, model2) => string.CompareOrdinal(model1.Path, model2.Path)))
                .ObserveOn(_schedulerProvider.MainThreadScheduler)
                .Bind(out _watchedPaths)
                .Subscribe()
                .DisposeWith(_disposables);

            Continue = ReactiveCommand.CreateFromObservable(ExecuteContinue);
            ContinueInteraction = new Interaction<Unit, Unit>();

            Cancel = ReactiveCommand.CreateFromObservable(ExecuteCancel);
            CancelInteraction = new Interaction<Unit, Unit>();

            _foldersObservableCollection = new ObservableCollectionExtended<FolderRuleViewModel>();

            _currentFolderManagementRules = Observable
                .StartAsync(async () => await _folderRulesManagementService
                    .GetFolderManagementRules()
                    .SelectMany(rule => rule)
                    .Do(rule =>
                    {
                        if (rule.Action == FolderRuleActionEnum.Always) _watchedPathsSourceCache.AddOrUpdate(rule);
                    })
                    .ToDictionary(rule => rule.Path, rule => rule.Action));

            _folderRuleViewModelDictionary = new ConcurrentDictionary<string, FolderRuleViewModel>();
        }

        public IObservable<Unit> Initialize()
        {
            return _currentFolderManagementRules
                .SelectMany(folderRules =>
                {
                    return _driveInfoFactory
                        .GetDrives()
                        .ToObservable()
                        .SubscribeOn(_schedulerProvider.TaskPool)
                        .Where(driveInfo => driveInfo.DriveType == DriveType.Fixed)
                        .Where(driveInfo => driveInfo.IsReady)
                        .Select(driveInfo => driveInfo.RootDirectory)
                        .SelectMany(async directoryInfo =>
                        {
                            var customFolderRuleInput = CreateCustomFolderRuleInput(directoryInfo, folderRules);

                            var tracers = new Dictionary<string, FolderRuleViewModel>
                            {
                                {customFolderRuleInput.Path, customFolderRuleInput}
                            };

                            while (tracers.Any())
                            {
                                var currentTracers = tracers.Select(tracerPair =>
                                {
                                    var isRule = folderRules.ContainsKey(directoryInfo.FullName);

                                    var isParentOfRule = !isRule &&
                                                         folderRules.Any(pair =>
                                                             pair.Value != FolderRuleActionEnum.Remove
                                                             && pair.Key.Length > tracerPair.Value.Path.Length
                                                             && pair.Key.StartsWith(tracerPair.Value.Path));

                                    return (tracerPair, isRule, isParentOfRule);
                                }).ToArray();

                                foreach (var currentTracer in currentTracers)
                                {
                                    tracers.Remove(currentTracer.Item1.Key);

                                    var currentFolderRuleViewModel = currentTracer.Item1.Value;

                                    var populateFolderRuleInput =
                                        PopulateFolderRuleInput(currentFolderRuleViewModel);

                                    if (currentTracer.isRule || currentTracer.isParentOfRule)
                                    {
                                        await populateFolderRuleInput;

                                        foreach (var child in currentFolderRuleViewModel.Children)
                                            tracers.Add(child.Path, child);
                                    }
                                    else
                                    {
                                        populateFolderRuleInput.Subscribe();
                                    }
                                }
                            }

                            return customFolderRuleInput;
                        });
                })
                .ToArray()
                .ObserveOn(_schedulerProvider.MainThreadScheduler)
                .Select(folderRuleViewModels =>
                {
                    _foldersObservableCollection.Add(folderRuleViewModels.OrderBy(model => model.Path));
                    return Unit.Default;
                })
                .LastAsync();
        }

        public IReadOnlyDictionary<string, FolderRuleViewModel> FolderRuleViewModelDictionary =>
            _folderRuleViewModelDictionary;

        public ReadOnlyObservableCollection<FolderRule> WatchedPaths => _watchedPaths;

        public Interaction<Unit, Unit> ContinueInteraction { get; }

        public ReactiveCommand<Unit, Unit> Continue { get; }

        public Interaction<Unit, Unit> CancelInteraction { get; }

        public ReactiveCommand<Unit, Unit> Cancel { get; }

        public FolderRuleViewModel SelectedItem
        {
            get => _selectedItem;
            set => this.RaiseAndSetIfChanged(ref _selectedItem, value);
        }

        public bool HideUnselected
        {
            get => _hideUnselected;
            set => this.RaiseAndSetIfChanged(ref _hideUnselected, value);
        }

        public IObservableCollection<FolderRuleViewModel> Folders => _foldersObservableCollection;

        public void Dispose()
        {
            _disposables?.Dispose();
        }

        IList<IFolderRuleInput> IManageFolderRulesViewModel.Folders => _foldersObservableCollection
            .Cast<IFolderRuleInput>()
            .ToArray();

        public IObservable<IDirectoryInfo[]> GetAccesibleChildDirectories(IDirectoryInfo directoryInfo)
        {
            return directoryInfo
                .GetDirectories()
                .ToObservable()
                .SubscribeOn(_schedulerProvider.TaskPool)
                .Where(info => !(info.Name.StartsWith(".") || info.Name.StartsWith("$")))
                .Where(_directoryInfoPermissionsService.IsReadable)
                .ToArray();
        }

        public IObservable<Unit> PopulateFolderRuleInput(FolderRuleViewModel folderRuleViewModel)
        {
            if (folderRuleViewModel.IsLoaded) return Observable.Return(Unit.Default);

            folderRuleViewModel.IsLoaded = true;

            return GetAccesibleChildDirectories(folderRuleViewModel.DirectoryInfo)
                .CombineLatest(_currentFolderManagementRules,
                    (directoryInfos, folderRules) => (directoryInfos, folderRules))
                .SelectMany(tuple => tuple.directoryInfos.Select(directoryInfo => (directoryInfo, tuple.folderRules)))
                .Select(tuple => CreateCustomFolderRuleInput(tuple.directoryInfo,
                    tuple.folderRules,
                    folderRuleViewModel.FolderRuleAction))
                .ObserveOn(_schedulerProvider.MainThreadScheduler)
                .Select(item =>
                {
                    folderRuleViewModel.Children.Add(item);
                    return Unit.Default;
                })
                .LastOrDefaultAsync();
        }

        private FolderRuleViewModel CreateCustomFolderRuleInput(IDirectoryInfo directoryInfo,
            IDictionary<string, FolderRuleActionEnum> folderRules,
            FolderRuleActionEnum defaultFolderRuleAction = FolderRuleActionEnum.Remove,
            bool forcePopulateChildren = false)
        {
            var folderRuleInput = new FolderRuleViewModel(directoryInfo);
            _folderRuleViewModelDictionary.TryAdd(directoryInfo.FullName, folderRuleInput);

            if (folderRules.TryGetValue(directoryInfo.FullName, out var folderRuleAction))
            {
                folderRuleInput.FolderRuleAction = folderRuleAction;
            }
            else
            {
                folderRuleInput.FolderRuleAction = defaultFolderRuleAction;

                var anyParentOfRule = folderRules.Any(s => s.Value != FolderRuleActionEnum.Remove
                                                           && s.Key.Length > directoryInfo.FullName.Length
                                                           && s.Key.StartsWith(directoryInfo.FullName));

                if (anyParentOfRule)
                    folderRuleInput.ShouldShowReason =
                        FolderRuleViewModel.ShouldShowChildReasonEnum.IsParentOfRule;
            }

            folderRuleInput.WhenPropertyChanged(model => model.FolderRuleAction, false)
                .Select(value =>
                {
                    var manageFolderViewModels = ((IManageFolderRulesViewModel) this).Folders;
                    return FolderRulesFactory.ComputeRuleset(manageFolderViewModels);
                })
                .ObserveOn(_schedulerProvider.MainThreadScheduler)
                .Subscribe(items =>
                {
                    _watchedPathsSourceCache.Edit(updater =>
                    {
                        updater.Clear();
                        updater.AddOrUpdate(items);
                    });
                })
                .DisposeWith(_disposables);

            return folderRuleInput;
        }

        private IObservable<Unit> ExecuteCancel()
        {
            return CancelInteraction.Handle(Unit.Default)
                .SubscribeOn(_schedulerProvider.TaskPool)
                .Select(unit => unit);
        }

        private IObservable<Unit> ExecuteContinue()
        {
            return ContinueInteraction.Handle(Unit.Default)
                .SubscribeOn(_schedulerProvider.TaskPool)
                .Select(unit => unit);
        }
    }
}