using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using SonOfPicasso.Core.Scheduling;
using SonOfPicasso.UI.ViewModels.Abstract;

namespace SonOfPicasso.UI.ViewModels
{
    public class FolderViewModel
    {
        public FolderViewModel(IDirectoryInfo directoryInfo, ObservableCollectionExtended<FolderViewModel> observableCollectionExtended)
        {
            Path = directoryInfo.FullName;
            Children = observableCollectionExtended;
        }

        public IObservableCollection<FolderViewModel> Children { get; }

        public string Path { get; }
    }

    public class FolderManagementViewModel: ViewModelBase
    {
        private readonly IFileSystem _fileSystem;
        private readonly ISchedulerProvider _schedulerProvider;

        public FolderManagementViewModel(ViewModelActivator activator, IFileSystem fileSystem, ISchedulerProvider schedulerProvider) : base(activator)
        {
            _fileSystem = fileSystem;
            _schedulerProvider = schedulerProvider;
            _foldersSourceCache = new SourceCache<FolderViewModel, string>(model => model.Path);

            this.WhenActivated(d =>
            {
                _foldersSourceCache
                    .Connect()
                    .ObserveOn(_schedulerProvider.MainThreadScheduler)
                    .Bind(Folders)
                    .Subscribe()
                    .DisposeWith(d);

                _foldersSourceCache.PopulateFrom(GetFolderViewModels());
            });
        }

        private IObservable<FolderViewModel> GetFolderViewModels()
        {
            return Observable.Create<FolderViewModel>(observer =>
            {
                var folderViewModels = _fileSystem.DriveInfo.GetDrives()
                    .Select(driveInfo => driveInfo.RootDirectory)
                    .Select(CreateFolderViewModel);

                foreach (var folderViewModel in folderViewModels)
                {
                    observer.OnNext(folderViewModel);
                }

                observer.OnCompleted();

                return Disposable.Empty;
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

        internal readonly SourceCache<FolderViewModel, string> _foldersSourceCache;

        public IObservableCollection<FolderViewModel> Folders { get; } =
            new ObservableCollectionExtended<FolderViewModel>();
    }
}
