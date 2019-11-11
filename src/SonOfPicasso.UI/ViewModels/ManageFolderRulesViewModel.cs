using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Akavache;
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
        private readonly ConcurrentDictionary<string, IObservable<IDirectoryInfo[]>> _childDirectoryLookup;
        private readonly IDirectoryInfoPermissionsService _directoryInfoPermissionsService;
        private readonly CompositeDisposable _disposables;
        private readonly IDriveInfoFactory _driveInfoFactory;
        private readonly IFileSystem _fileSystem;
        private readonly IFolderRulesManagementService _folderRulesManagementService;
        private readonly ObservableCollectionExtended<CustomFolderRuleInput> _foldersObservableCollection;
        private readonly ILogger _logger;
        private readonly Subject<FolderRuleInput> _onChangedSubject;
        private readonly ISchedulerProvider _schedulerProvider;
        private readonly ObservableCollectionExtended<string> _watchedPaths;

        private bool _hideUnselected;
        private CustomFolderRuleInput _selectedItem;
        private IObservable<IList<FolderRule>> _currentFolderManagementRules;
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

            _onChangedSubject = new Subject<FolderRuleInput>();
            _onChangedSubject
                .Select(model => FolderRulesFactory
                    .ComputeRuleset(_foldersObservableCollection)
                    .Where(rule => rule.Action == FolderRuleActionEnum.Always)
                    .Select(rule => rule.Path)
                    .ToArray())
                .ObserveOn(_schedulerProvider.MainThreadScheduler)
                .Subscribe(strings =>
                {
                    _watchedPaths.Clear();
                    _watchedPaths.AddRange(strings);
                });

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

        public void Dispose()
        {
            _disposables?.Dispose();
        }

        public bool HideUnselected
        {
            get => _hideUnselected;
            set => this.RaiseAndSetIfChanged(ref _hideUnselected, value);
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

        public IObservableCollection<CustomFolderRuleInput> Folders => _foldersObservableCollection;

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
                            var customFolderRuleInput = new CustomFolderRuleInput(directoryInfo);
                            PopulateFolderRuleInput(customFolderRuleInput);
                            return customFolderRuleInput;
                        });
                });
        }

        public void PopulateFolderRuleInput(CustomFolderRuleInput customFolderRuleInput)
        {
            if (customFolderRuleInput.IsLoaded)
            {
                return;
            }

            customFolderRuleInput.IsLoaded = true;

            GetAccesibleChildDirectories(customFolderRuleInput.DirectoryInfo)
                .CombineLatest(currentFolderManagementRules, (directoryInfos, folderRules) => (directoryInfos, folderRules))
                .SelectMany(tuple => tuple.directoryInfos.Select(directoryInfo => (directoryInfo, tuple.folderRules)))
                .Select(tuple =>
                {
                    var folderRuleInput = new CustomFolderRuleInput(tuple.directoryInfo);

                    if (tuple.folderRules.TryGetValue(tuple.directoryInfo.FullName, out var folderRuleAction))
                        folderRuleInput.FolderRuleAction = folderRuleAction;
                    else
                        folderRuleInput.FolderRuleAction = customFolderRuleInput.FolderRuleAction;

                    return folderRuleInput;
                })
                .ObserveOn(_schedulerProvider.MainThreadScheduler)
                .Subscribe(item => customFolderRuleInput.Children.Add(item));
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

    public class CustomFolderRuleInput : ReactiveObject, IFolderRuleInput
    {
        private FolderRuleActionEnum _folderRuleAction;
        private bool _isLoaded;
        public IDirectoryInfo DirectoryInfo { get; }

        public CustomFolderRuleInput(IDirectoryInfo directoryInfo)
        {
            DirectoryInfo = directoryInfo;
            Children = new ObservableCollectionExtended<CustomFolderRuleInput>();
        }

        public string Name => DirectoryInfo.Name;

        public string Path => DirectoryInfo.FullName;

        public FolderRuleActionEnum FolderRuleAction
        {
            get => _folderRuleAction;
            set => this.RaiseAndSetIfChanged(ref _folderRuleAction, value);
        }

        public bool IsLoaded
        {
            get => _isLoaded;
            set => this.RaiseAndSetIfChanged(ref _isLoaded, value);
        }

        public IObservableCollection<CustomFolderRuleInput> Children { get; }

        IList<IFolderRuleInput> IFolderRuleInput.Children => Children.Cast<IFolderRuleInput>().ToArray();
    }
}