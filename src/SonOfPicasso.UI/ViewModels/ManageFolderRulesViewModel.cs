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
        private readonly ObservableCollectionExtended<FolderRuleViewModel> _foldersObservableCollection;
        private readonly ILogger _logger;
        private readonly Func<FolderRuleViewModel> _manageFolderViewModelFactory;
        private readonly Subject<FolderRuleViewModel> _onChangedSubject;
        private readonly ISchedulerProvider _schedulerProvider;
        private readonly ObservableCollectionExtended<string> _watchedPaths;

        private bool _hideUnselected;
        private FolderRuleViewModel _selectedItem;

        public ManageFolderRulesViewModel(ViewModelActivator activator,
            IFileSystem fileSystem,
            IDriveInfoFactory driveInfoFactory,
            IDirectoryInfoPermissionsService directoryInfoPermissionsService,
            IFolderRulesManagementService folderRulesManagementService,
            ISchedulerProvider schedulerProvider,
            ILogger logger,
            Func<FolderRuleViewModel> manageFolderViewModelFactory
        ) : base(activator)
        {
            _fileSystem = fileSystem;
            _driveInfoFactory = driveInfoFactory;
            _directoryInfoPermissionsService = directoryInfoPermissionsService;
            _folderRulesManagementService = folderRulesManagementService;
            _schedulerProvider = schedulerProvider;
            _logger = logger;
            _manageFolderViewModelFactory = manageFolderViewModelFactory;

            _childDirectoryLookup = new ConcurrentDictionary<string, IObservable<IDirectoryInfo[]>>();

            Continue = ReactiveCommand.CreateFromObservable(ExecuteContinue);
            ContinueInteraction = new Interaction<Unit, Unit>();

            Cancel = ReactiveCommand.CreateFromObservable(ExecuteCancel);
            CancelInteraction = new Interaction<Unit, Unit>();

            _foldersObservableCollection = new ObservableCollectionExtended<FolderRuleViewModel>();

            _disposables = new CompositeDisposable();

            _onChangedSubject = new Subject<FolderRuleViewModel>();
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

            var observable = Observable
                .Start(() => _folderRulesManagementService.GetFolderManagementRules(),
                    schedulerProvider.TaskPool)
                .SelectMany(o => o);

            observable
                .Select(list => list.Where(rule => rule.Action == FolderRuleActionEnum.Always).Select(rule => rule.Path).ToArray())
                .ObserveOn(_schedulerProvider.MainThreadScheduler)
                .Subscribe(items =>
                {
                    _watchedPaths.AddRange(items);
                })
                .DisposeWith(_disposables);

            GetFolderViewModels(observable)
                .ObserveOn(_schedulerProvider.MainThreadScheduler)
                .Subscribe(item => _foldersObservableCollection.Add(item))
                .DisposeWith(_disposables);
        }

        public IObservableCollection<string> WatchedPaths => _watchedPaths;

        public Interaction<Unit, Unit> ContinueInteraction { get; }

        public ReactiveCommand<Unit, Unit> Continue { get; }

        public Interaction<Unit, Unit> CancelInteraction { get; }

        public ReactiveCommand<Unit, Unit> Cancel { get; }

        public FolderRuleViewModel SelectedItem
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

        public IObservableCollection<FolderRuleViewModel> Folders => _foldersObservableCollection;

        private IObservable<FolderRuleViewModel> GetFolderViewModels(IObservable<IList<FolderRule>> observable)
        {
            return observable
                .Select(list => list.ToDictionary(rule => rule.Path, rule => rule.Action))
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
                            var folderViewModel = _manageFolderViewModelFactory();
                            folderViewModel.Initialize(this,
                                directoryInfo,
                                managementRulesDictionary,
                                FolderRuleActionEnum.Remove,
                                _onChangedSubject);
                            return folderViewModel;
                        });
                });
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