using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using DynamicData.Binding;
using ReactiveUI;
using SonOfPicasso.UI.ViewModels.Abstract;

namespace SonOfPicasso.UI.ViewModels
{
    public class FolderManagementViewModel: ViewModelBase
    {
        public class FolderViewModel
        {
            public FolderViewModel(IDirectoryInfo directoryInfo)
            {
                Path = directoryInfo.FullName;
                Children = new ObservableCollectionExtended<FolderViewModel>(directoryInfo.EnumerateDirectories()
                    .Select(info => new FolderViewModel(info)));


            }

            public IObservableCollection<FolderViewModel> Children { get; }

            public string Path { get; }
        }

        private readonly IFileSystem _fileSystem;

        public FolderManagementViewModel(ViewModelActivator activator, IFileSystem fileSystem) : base(activator)
        {
            _fileSystem = fileSystem;

            this.WhenActivated((CompositeDisposable disposable) =>
            {
                var driveInfos = _fileSystem.DriveInfo.GetDrives()
                    .Select(driveInfo => driveInfo.RootDirectory)
                    .Select(directoryInfo => new FolderViewModel(directoryInfo))
                    .ToArray();
            });
        }
    }
}
