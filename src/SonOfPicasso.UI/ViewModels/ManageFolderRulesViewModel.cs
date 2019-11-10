using System;
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
using SonOfPicasso.UI.Interfaces;
using SonOfPicasso.UI.ViewModels.Abstract;

namespace SonOfPicasso.UI.ViewModels
{
    public class ManageFolderRulesViewModel : ViewModelBase, IManageFolderRulesViewModel
    {
        private readonly IDirectoryInfoPermissionsService _directoryInfoPermissionsService;
        private readonly IDriveInfoFactory _driveInfoFactory;
        private readonly IFileSystem _fileSystem;
        private readonly IFolderRulesManagementService _folderRulesManagementService;
        private readonly ObservableCollectionExtended<FolderRuleViewModel> _foldersObservableCollection;

        private readonly ILogger _logger;
        private readonly Func<FolderRuleViewModel> _manageFolderViewModelFactory;
        private readonly ISchedulerProvider _schedulerProvider;
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

            Continue = ReactiveCommand.CreateFromObservable(ExecuteContinue);
            ContinueInteraction = new Interaction<Unit, Unit>();

            Cancel = ReactiveCommand.CreateFromObservable(ExecuteCancel);
            CancelInteraction = new Interaction<Unit, Unit>();

            _foldersObservableCollection = new ObservableCollectionExtended<FolderRuleViewModel>();

            this.WhenActivated(d =>
            {
                GetFolderViewModels()
                    .ToList()
                    .ObserveOn(_schedulerProvider.MainThreadScheduler)
                    .Subscribe(items => _foldersObservableCollection.AddRange(items))
                    .DisposeWith(d);
            });
        }

        public bool HideUnselected
        {
            get => _hideUnselected;
            set => this.RaiseAndSetIfChanged(ref _hideUnselected, value);
        }

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

        private IObservable<FolderRuleViewModel> GetFolderViewModels()
        {
            return Observable.Defer(() =>
                {
                    var folderManagementRules = _folderRulesManagementService.GetFolderManagementRules()
                        .Select(list => list.ToDictionary(rule => rule.Path, rule => rule.Action));

                    return _driveInfoFactory.GetDrives()
                        .Where(driveInfo => driveInfo.DriveType == DriveType.Fixed)
                        .Where(driveInfo => driveInfo.IsReady)
                        .Select(driveInfo => driveInfo.RootDirectory)
                        .ToObservable()
                        .ToArray()
                        .CombineLatest(folderManagementRules, (directoryInfos, folderManagmentRules) =>
                        {
                            return directoryInfos
                                .ToObservable()
                                .Select(directoryInfo =>
                                {
                                    var folderViewModel = _manageFolderViewModelFactory();
                                    folderViewModel.Initialize(this, directoryInfo, folderManagmentRules);
                                    return folderViewModel;
                                });
                        })
                        .SelectMany(observable1 => observable1);
                })
                .SubscribeOn(_schedulerProvider.TaskPool);
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