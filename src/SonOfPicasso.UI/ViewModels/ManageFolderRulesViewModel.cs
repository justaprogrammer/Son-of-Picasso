using System;
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
using Serilog;
using SonOfPicasso.Core.Interfaces;
using SonOfPicasso.Core.Scheduling;
using SonOfPicasso.Core.Services;
using SonOfPicasso.Data.Model;
using SonOfPicasso.UI.Interfaces;
using SonOfPicasso.UI.ViewModels.Abstract;

namespace SonOfPicasso.UI.ViewModels
{
    public class ManageFolderRulesViewModel : ViewModelBase, IManageFolderRulesViewModel, IDisposable
    {
        private readonly IDirectoryInfoPermissionsService _directoryInfoPermissionsService;
        private readonly CompositeDisposable _disposables;
        private readonly IDriveInfoFactory _driveInfoFactory;
        private readonly IFileSystem _fileSystem;
        private readonly IFolderRulesManagementService _folderRulesManagementService;
        private readonly ObservableCollectionExtended<FolderRuleInputViewModel> _foldersObservableCollection;
        private readonly ILogger _logger;
        private readonly ISchedulerProvider _schedulerProvider;
        private readonly ReadOnlyObservableCollection<FolderRule> _watchedPaths;

        private readonly SourceCache<FolderRule, string> _watchedPathsSourceCache;
        private IObservable<IDictionary<string, FolderRuleActionEnum>> _currentFolderManagementRules;

        private bool _hideUnselected;
        private FolderRuleInputViewModel _selectedItem;

        public ManageFolderRulesViewModel(ViewModelActivator activator,
            IFileSystem fileSystem,
            IDriveInfoFactory driveInfoFactory,
            IDirectoryInfoPermissionsService directoryInfoPermissionsService,
            IFolderRulesManagementService folderRulesManagementService,
            ISchedulerProvider schedulerProvider,
            ILogger logger
        ) : base(activator)
        {
            _fileSystem = fileSystem;
            _driveInfoFactory = driveInfoFactory;
            _directoryInfoPermissionsService = directoryInfoPermissionsService;
            _folderRulesManagementService = folderRulesManagementService;
            _schedulerProvider = schedulerProvider;
            _logger = logger;
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

            _foldersObservableCollection = new ObservableCollectionExtended<FolderRuleInputViewModel>();

            GetFolderViewModels()
                .ObserveOn(_schedulerProvider.MainThreadScheduler)
                .Subscribe(item => _foldersObservableCollection.Add(item))
                .DisposeWith(_disposables);
        }

        public ReadOnlyObservableCollection<FolderRule> WatchedPaths => _watchedPaths;

        public Interaction<Unit, Unit> ContinueInteraction { get; }

        public ReactiveCommand<Unit, Unit> Continue { get; }

        public Interaction<Unit, Unit> CancelInteraction { get; }

        public ReactiveCommand<Unit, Unit> Cancel { get; }

        public FolderRuleInputViewModel SelectedItem
        {
            get => _selectedItem;
            set => this.RaiseAndSetIfChanged(ref _selectedItem, value);
        }

        public bool HideUnselected
        {
            get => _hideUnselected;
            set => this.RaiseAndSetIfChanged(ref _hideUnselected, value);
        }

        public IObservableCollection<FolderRuleInputViewModel> Folders => _foldersObservableCollection;

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
                .Where(info => !info.Name.StartsWith("."))
                .Where(_directoryInfoPermissionsService.IsReadable)
                .ToArray();
        }

        private IObservable<FolderRuleInputViewModel> GetFolderViewModels()
        {
            _currentFolderManagementRules ??= Observable
                .StartAsync(async () => await _folderRulesManagementService
                    .GetFolderManagementRules()
                    .SelectMany(rule => rule)
                    .Do(rule =>
                    {
                        if (rule.Action == FolderRuleActionEnum.Always) _watchedPathsSourceCache.AddOrUpdate(rule);
                    })
                    .ToDictionary(rule => rule.Path, rule => rule.Action));

            return _currentFolderManagementRules
                .SelectMany(managementRulesDictionary =>
                {
                    return _driveInfoFactory.GetDrives()
                        .ToObservable()
                        .SubscribeOn(_schedulerProvider.TaskPool)
                        .Where(driveInfo => driveInfo.DriveType == DriveType.Fixed)
                        .Where(driveInfo => driveInfo.IsReady)
                        .Select(driveInfo => driveInfo.RootDirectory)
                        .Select(directoryInfo =>
                        {
                            var customFolderRuleInput =
                                CreateCustomFolderRuleInput(directoryInfo, managementRulesDictionary);
                            PopulateFolderRuleInput(customFolderRuleInput);
                            return customFolderRuleInput;
                        });
                });
        }

        public void PopulateFolderRuleInput(FolderRuleInputViewModel folderRuleInputViewModel)
        {
            if (folderRuleInputViewModel.IsLoaded) return;

            folderRuleInputViewModel.IsLoaded = true;

            GetAccesibleChildDirectories(folderRuleInputViewModel.DirectoryInfo)
                .CombineLatest(_currentFolderManagementRules,
                    (directoryInfos, folderRules) => (directoryInfos, folderRules))
                .SelectMany(tuple => tuple.directoryInfos.Select(directoryInfo => (directoryInfo, tuple.folderRules)))
                .Select(tuple => CreateCustomFolderRuleInput(tuple.directoryInfo, tuple.folderRules,
                    folderRuleInputViewModel.FolderRuleAction))
                .ObserveOn(_schedulerProvider.MainThreadScheduler)
                .Subscribe(item => folderRuleInputViewModel.Children.Add(item));
        }

        private FolderRuleInputViewModel CreateCustomFolderRuleInput(IDirectoryInfo directoryInfo,
            IDictionary<string, FolderRuleActionEnum> folderRules,
            FolderRuleActionEnum defaultFolderRuleAction = FolderRuleActionEnum.Remove)
        {
            var folderRuleInput = new FolderRuleInputViewModel(directoryInfo);

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
                        FolderRuleInputViewModel.ShouldShowChildReasonEnum.IsParentOfRule;
            }

            folderRuleInput.WhenPropertyChanged(model => model.FolderRuleAction, false)
                .Select(value =>
                {
                    var manageFolderViewModels = ((IManageFolderRulesViewModel) this).Folders;
                    var items = FolderRulesFactory.ComputeRuleset(manageFolderViewModels);
                    return items;
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

    public class FolderRuleInputViewModel : ReactiveObject, IFolderRuleInput, IDisposable
    {
        public enum ShouldShowChildReasonEnum
        {
            ShouldNot,
            IsParentOfRule
        }

        private readonly CompositeDisposable _disposables;
        private readonly ObservableAsPropertyHelper<bool> _shouldShowPropertyHelper;
        private FolderRuleActionEnum _folderRuleAction;
        private bool _isLoaded;
        private ShouldShowChildReasonEnum _shouldShowReason = ShouldShowChildReasonEnum.ShouldNot;

        public FolderRuleInputViewModel(IDirectoryInfo directoryInfo)
        {
            DirectoryInfo = directoryInfo;
            Children = new ObservableCollectionExtended<FolderRuleInputViewModel>();

            _disposables = new CompositeDisposable();

            _shouldShowPropertyHelper = this.WhenPropertyChanged(model => model.ShouldShowReason)
                .Select(propertyValue => propertyValue.Value != ShouldShowChildReasonEnum.ShouldNot)
                .ToProperty(this, model => model.ShouldShow);

            this
                .WhenPropertyChanged(model => model.FolderRuleAction, false)
                .Subscribe(propertyValue =>
                {
                    foreach (var customFolderRuleInput in Children)
                        customFolderRuleInput.FolderRuleAction = propertyValue.Value;
                })
                .DisposeWith(_disposables);
        }

        public IDirectoryInfo DirectoryInfo { get; }

        public bool ShouldShow => _shouldShowPropertyHelper.Value;

        public string Name => DirectoryInfo.Name;

        public bool IsLoaded
        {
            get => _isLoaded;
            set => this.RaiseAndSetIfChanged(ref _isLoaded, value);
        }

        public IObservableCollection<FolderRuleInputViewModel> Children { get; }

        public ShouldShowChildReasonEnum ShouldShowReason
        {
            get => _shouldShowReason;
            set => this.RaiseAndSetIfChanged(ref _shouldShowReason, value);
        }

        public void Dispose()
        {
            _shouldShowPropertyHelper?.Dispose();
            _disposables?.Dispose();
        }

        public string Path => DirectoryInfo.FullName;

        public FolderRuleActionEnum FolderRuleAction
        {
            get => _folderRuleAction;
            set => this.RaiseAndSetIfChanged(ref _folderRuleAction, value);
        }

        IList<IFolderRuleInput> IFolderRuleInput.Children => Children.Cast<IFolderRuleInput>().ToArray();
    }
}