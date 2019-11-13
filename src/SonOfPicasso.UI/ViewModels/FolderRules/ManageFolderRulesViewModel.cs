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
using System.Reactive.Subjects;
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
        private readonly Subject<IDictionary<string, FolderRuleActionEnum>> _currentFolderManagementRulesSubject;
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

            _currentFolderManagementRulesSubject = new Subject<IDictionary<string, FolderRuleActionEnum>>();
            var currentFolderManagementRules = _currentFolderManagementRulesSubject
                .AsObservable()
                .Replay(1);

            CurrentFolderManagementRules = currentFolderManagementRules;
            currentFolderManagementRules.Connect();

            _folderRulesManagementService
                .GetFolderManagementRules()
                .SelectMany(rule => rule)
                .ToDictionary(rule => rule.Path, rule => rule.Action)
                .Subscribe(enums =>
                {
                    _currentFolderManagementRulesSubject.OnNext(enums);
                })
                .DisposeWith(_disposables);

            _folderRuleViewModelDictionary = new ConcurrentDictionary<string, FolderRuleViewModel>();
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

        public IObservableCollection<FolderRuleViewModel> Folders => _foldersObservableCollection;

        public void Dispose()
        {
            _disposables?.Dispose();
        }

        public IObservable<IDictionary<string, FolderRuleActionEnum>> CurrentFolderManagementRules { get; }

        public IObservable<Unit> Initialize()
        {
            return CurrentFolderManagementRules
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

        public bool HideUnselected
        {
            get => _hideUnselected;
            set => this.RaiseAndSetIfChanged(ref _hideUnselected, value);
        }

        IList<IFolderRuleInput> IManageFolderRulesViewModel.Folders => _foldersObservableCollection
            .Cast<IFolderRuleInput>()
            .ToArray();

        public IObservable<Unit> PopulateFolderRuleInput(FolderRuleViewModel folderRuleViewModel)
        {
            if (folderRuleViewModel.IsLoaded) return Observable.Return(Unit.Default);

            folderRuleViewModel.IsLoaded = true;

            return folderRuleViewModel
                .DirectoryInfo
                .GetDirectories()
                .ToObservable()
                .SubscribeOn(_schedulerProvider.TaskPool)
                .Where(info => !(info.Name.StartsWith(".") || info.Name.StartsWith("$")))
                .Where(_directoryInfoPermissionsService.IsReadable)
                .ToArray()
                .CombineLatest(CurrentFolderManagementRules,
                    (directoryInfos, folderRules) => (directoryInfos, folderRules))
                .SelectMany(tuple => tuple.directoryInfos.Select(directoryInfo => (directoryInfo, tuple.folderRules)))
                .Select(tuple => CreateCustomFolderRuleInput(tuple.directoryInfo, tuple.folderRules, folderRuleViewModel))
                .ObserveOn(_schedulerProvider.MainThreadScheduler)
                .Select(item =>
                {
                    folderRuleViewModel.AddChild(item);
                    return Unit.Default;
                })
                .LastOrDefaultAsync();
        }

        private FolderRuleViewModel CreateCustomFolderRuleInput(IDirectoryInfo directoryInfo,
            IDictionary<string, FolderRuleActionEnum> folderRules,
            FolderRuleViewModel parentFolderRuleViewModel = null)
        {
            var folderRuleInput = new FolderRuleViewModel(this, directoryInfo, folderRules, parentFolderRuleViewModel, _schedulerProvider);
            _folderRuleViewModelDictionary.TryAdd(directoryInfo.FullName, folderRuleInput);

            folderRuleInput.WhenPropertyChanged(model => model.FolderRuleAction, false)
                .Select(value =>
                {
                    var manageFolderViewModels = ((IManageFolderRulesViewModel) this).Folders;
                    return FolderRulesFactory.ComputeRuleset(manageFolderViewModels);
                })
                .ObserveOn(_schedulerProvider.MainThreadScheduler)
                .Subscribe(items =>
                {
                    var itemsArray = items.ToArray();

                    var dictionary = itemsArray.ToDictionary(rule => rule.Path, rule => rule.Action);
                    _currentFolderManagementRulesSubject.OnNext(dictionary);

                    _watchedPathsSourceCache.Edit(updater =>
                    {
                        updater.Clear();
                        updater.AddOrUpdate(itemsArray);
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