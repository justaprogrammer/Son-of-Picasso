using System;
using System.Collections.Generic;
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
using Serilog;
using SonOfPicasso.Core.Scheduling;
using SonOfPicasso.Data.Model;
using SonOfPicasso.UI.ViewModels.Abstract;

namespace SonOfPicasso.UI.ViewModels
{
    public class FolderRulesViewModel : ViewModelBase
    {
        private readonly IDriveInfoFactory _driveInfoFactory;
        private readonly IDirectoryInfoPermissionsService _directoryInfoPermissionsService;
        private readonly IFileSystem _fileSystem;
        private readonly ObservableCollectionExtended<ManageFolderViewModel> _foldersObservableCollection;

        private readonly ILogger _logger;
        private readonly Func<ManageFolderViewModel> _manageFolderViewModelFactory;
        private readonly ISchedulerProvider _schedulerProvider;
        private ManageFolderViewModel _selectedItem;

        public FolderRulesViewModel(ViewModelActivator activator,
            IFileSystem fileSystem,
            IDriveInfoFactory driveInfoFactory,
            IDirectoryInfoPermissionsService directoryInfoPermissionsService,
            ISchedulerProvider schedulerProvider,
            ILogger logger,
            Func<ManageFolderViewModel> manageFolderViewModelFactory
        ) : base(activator)
        {
            _fileSystem = fileSystem;
            _driveInfoFactory = driveInfoFactory;
            _directoryInfoPermissionsService = directoryInfoPermissionsService;
            _schedulerProvider = schedulerProvider;
            _logger = logger;
            _manageFolderViewModelFactory = manageFolderViewModelFactory;

            Continue = ReactiveCommand.CreateFromObservable(ExecuteContinue);
            ContinueInteraction = new Interaction<Unit, Unit>();

            Cancel = ReactiveCommand.CreateFromObservable(ExecuteCancel);
            CancelInteraction = new Interaction<Unit, Unit>();

            _foldersObservableCollection = new ObservableCollectionExtended<ManageFolderViewModel>();

            this.WhenActivated(d =>
            {
                GetFolderViewModels()
                    .ToList()
                    .ObserveOn(_schedulerProvider.MainThreadScheduler)
                    .Subscribe(items => _foldersObservableCollection.AddRange(items))
                    .DisposeWith(d);
            });
        }

        public Interaction<Unit, Unit> ContinueInteraction { get; set; }

        public ReactiveCommand<Unit, Unit> Continue { get; }

        public Interaction<Unit, Unit> CancelInteraction { get; set; }

        public ReactiveCommand<Unit, Unit> Cancel { get; }

        public ManageFolderViewModel SelectedItem
        {
            get => _selectedItem;
            set => this.RaiseAndSetIfChanged(ref _selectedItem, value);
        }

        public IObservableCollection<ManageFolderViewModel> Folders => _foldersObservableCollection;

        private IObservable<ManageFolderViewModel> GetFolderViewModels()
        {
            return Observable.Defer(() =>
                {
                    return _driveInfoFactory.GetDrives()
                        .Where(driveInfo => driveInfo.DriveType == DriveType.Fixed)
                        .Where(driveInfo => driveInfo.IsReady)
                        .Select(driveInfo => driveInfo.RootDirectory)
                        .Select(directoryInfo =>
                        {
                            var folderViewModel = _manageFolderViewModelFactory();
                            folderViewModel.Initialize(directoryInfo, FolderRuleActionEnum.Remove);
                            return folderViewModel;
                        })
                        .ToObservable();
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