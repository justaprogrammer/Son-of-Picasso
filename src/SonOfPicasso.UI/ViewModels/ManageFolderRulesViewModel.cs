using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData.Binding;
using ReactiveUI;
using Serilog;
using SonOfPicasso.Core.Interfaces;
using SonOfPicasso.Core.Scheduling;
using SonOfPicasso.Data.Model;
using SonOfPicasso.UI.Interfaces;
using SonOfPicasso.UI.ViewModels.Abstract;

namespace SonOfPicasso.UI.ViewModels
{
    public class ManageFolderRulesViewModel : ViewModelBase, IManageFolderRulesViewModel, IDisposable
    {
        private readonly ConcurrentDictionary<string, IObservable<IDirectoryInfo[]>> _childDirectoryLookup;
        private readonly IDirectoryInfoPermissionsService _directoryInfoPermissionsService;
        private readonly CompositeDisposable _disposables;
        private readonly IDriveInfoFactory _driveInfoFactory;
        private readonly IFileSystem _fileSystem;
        private readonly IFolderRulesManagementService _folderRulesManagementService;
        private readonly ObservableCollectionExtended<CustomFolderRuleInput> _foldersObservableCollection;
        private readonly ILogger _logger;
        private readonly ISchedulerProvider _schedulerProvider;
        private readonly ObservableCollectionExtended<string> _watchedPaths;
        private IObservable<IList<FolderRule>> _currentFolderManagementRules;

        private bool _hideUnselected;
        private CustomFolderRuleInput _selectedItem;
        private IObservable<IDictionary<string, FolderRuleActionEnum>> currentFolderManagementRules;

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

            _childDirectoryLookup = new ConcurrentDictionary<string, IObservable<IDirectoryInfo[]>>();

            Continue = ReactiveCommand.CreateFromObservable(ExecuteContinue);
            ContinueInteraction = new Interaction<Unit, Unit>();

            Cancel = ReactiveCommand.CreateFromObservable(ExecuteCancel);
            CancelInteraction = new Interaction<Unit, Unit>();

            _foldersObservableCollection = new ObservableCollectionExtended<CustomFolderRuleInput>();

            _disposables = new CompositeDisposable();

            _watchedPaths = new ObservableCollectionExtended<string>();

            GetFolderViewModels()
                .ObserveOn(_schedulerProvider.MainThreadScheduler)
                .Subscribe(item => _foldersObservableCollection.Add(item))
                .DisposeWith(_disposables);
        }

        public IObservableCollection<string> WatchedPaths => _watchedPaths;

        public Interaction<Unit, Unit> ContinueInteraction { get; }

        public ReactiveCommand<Unit, Unit> Continue { get; }

        public Interaction<Unit, Unit> CancelInteraction { get; }

        public ReactiveCommand<Unit, Unit> Cancel { get; }

        public CustomFolderRuleInput SelectedItem
        {
            get => _selectedItem;
            set => this.RaiseAndSetIfChanged(ref _selectedItem, value);
        }

        public bool HideUnselected
        {
            get => _hideUnselected;
            set => this.RaiseAndSetIfChanged(ref _hideUnselected, value);
        }

        public IObservableCollection<CustomFolderRuleInput> Folders => _foldersObservableCollection;

        public void Dispose()
        {
            _disposables?.Dispose();
        }

        public IObservable<IDirectoryInfo[]> GetAccesibleChildDirectories(IDirectoryInfo directoryInfo)
        {
            return _childDirectoryLookup.GetOrAdd(directoryInfo.FullName, s => Observable.Start(() => directoryInfo
                .GetDirectories()
                .Where(info => !info.Name.StartsWith("."))
                .Where(_directoryInfoPermissionsService.IsReadable)
                .OrderBy(info => info.Name)
                .ToArray()));
        }

        private IObservable<CustomFolderRuleInput> GetFolderViewModels()
        {
            currentFolderManagementRules ??= Observable
                .StartAsync(async () => await _folderRulesManagementService
                    .GetFolderManagementRules()
                    .SelectMany(rule => rule)
                    .ToDictionary(rule => rule.Path, rule => rule.Action));

            return currentFolderManagementRules
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

        public void PopulateFolderRuleInput(CustomFolderRuleInput customFolderRuleInput)
        {
            if (customFolderRuleInput.IsLoaded) return;

            customFolderRuleInput.IsLoaded = true;

            GetAccesibleChildDirectories(customFolderRuleInput.DirectoryInfo)
                .CombineLatest(currentFolderManagementRules,
                    (directoryInfos, folderRules) => (directoryInfos, folderRules))
                .SelectMany(tuple => tuple.directoryInfos.Select(directoryInfo => (directoryInfo, tuple.folderRules)))
                .Select(tuple => CreateCustomFolderRuleInput(tuple.directoryInfo, tuple.folderRules,
                    customFolderRuleInput.FolderRuleAction))
                .ObserveOn(_schedulerProvider.MainThreadScheduler)
                .Subscribe(item => customFolderRuleInput.Children.Add(item));
        }

        private static CustomFolderRuleInput CreateCustomFolderRuleInput(IDirectoryInfo directoryInfo,
            IDictionary<string, FolderRuleActionEnum> folderRules,
            FolderRuleActionEnum defaultFolderRuleAction = FolderRuleActionEnum.Remove)
        {
            var folderRuleInput = new CustomFolderRuleInput(directoryInfo);

            if (folderRules.TryGetValue(directoryInfo.FullName, out var folderRuleAction))
                folderRuleInput.FolderRuleAction = folderRuleAction;
            else
                folderRuleInput.FolderRuleAction = defaultFolderRuleAction;

            var anyChildHasRule = folderRules.Any(s => s.Value != FolderRuleActionEnum.Remove
                                                       && s.Key.StartsWith(directoryInfo.FullName));
            if (anyChildHasRule)
                folderRuleInput.ShouldShowReason = CustomFolderRuleInput.ShouldShowChildReasonEnum.ChildInRules;

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

    public class CustomFolderRuleInput : ReactiveObject, IFolderRuleInput, IDisposable
    {
        public enum ShouldShowChildReasonEnum
        {
            ShouldNot,
            ChildInRules
        }

        private readonly CompositeDisposable _disposables;
        private readonly ObservableAsPropertyHelper<bool> _shouldShowPropertyHelper;
        private FolderRuleActionEnum _folderRuleAction;
        private bool _isLoaded;
        private ShouldShowChildReasonEnum _shouldShowReason = ShouldShowChildReasonEnum.ShouldNot;

        public CustomFolderRuleInput(IDirectoryInfo directoryInfo)
        {
            DirectoryInfo = directoryInfo;
            Children = new ObservableCollectionExtended<CustomFolderRuleInput>();

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

        public IObservableCollection<CustomFolderRuleInput> Children { get; }

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