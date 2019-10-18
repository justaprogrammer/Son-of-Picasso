using System;
using System.IO.Abstractions;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using SonOfPicasso.Core.Scheduling;
using SonOfPicasso.UI.ViewModels.Abstract;

namespace SonOfPicasso.UI.ViewModels
{
    public class FolderViewModel
    {
        public FolderViewModel(IDirectoryInfo directoryInfo,
            ObservableCollectionExtended<FolderViewModel> observableCollectionExtended)
        {
            Path = directoryInfo.FullName;
            Children = observableCollectionExtended;
        }

        public IObservableCollection<FolderViewModel> Children { get; }

        public string Path { get; }
    }

    public class FolderManagementViewModel : ViewModelBase
    {
        private readonly IFileSystem _fileSystem;

        internal readonly SourceCache<FolderViewModel, string> _foldersSourceCache;
        private readonly ISchedulerProvider _schedulerProvider;
        private readonly ObservableCollectionExtended<FolderViewModel> _foldersObservableCollection;

        public FolderManagementViewModel(ViewModelActivator activator, IFileSystem fileSystem,
            ISchedulerProvider schedulerProvider) : base(activator)
        {
            _fileSystem = fileSystem;
            _schedulerProvider = schedulerProvider;
            _foldersSourceCache = new SourceCache<FolderViewModel, string>(model => model.Path);

            Continue = ReactiveCommand.CreateFromObservable(ExecuteContinue);
            ContinueInteraction = new Interaction<Unit, Unit>();

            Cancel = ReactiveCommand.CreateFromObservable(ExecuteCancel);
            CancelInteraction = new Interaction<Unit, Unit>();

            _foldersObservableCollection = new ObservableCollectionExtended<FolderViewModel>();

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

        public IObservableCollection<FolderViewModel> Folders => _foldersObservableCollection;

        private IObservable<FolderViewModel> GetFolderViewModels()
        {
            return Observable.Defer(() =>
            {
                return _fileSystem.DriveInfo.GetDrives()
                    .Select(driveInfo => driveInfo.RootDirectory)
                    .Select(CreateFolderViewModel)
                    .ToObservable();

            }).SubscribeOn(_schedulerProvider.TaskPool);
        }

        private FolderViewModel CreateFolderViewModel(IDirectoryInfo directoryInfo)
        {
            var enumerable = directoryInfo.EnumerateDirectories()
                .Select(CreateFolderViewModel);

            var children = new ObservableCollectionExtended<FolderViewModel>(enumerable);

            var folderViewModel = new FolderViewModel(directoryInfo, children);
            _foldersSourceCache.AddOrUpdate(folderViewModel);

            return folderViewModel;
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