using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using Autofac;
using DynamicData;
using ReactiveUI;
using Serilog;
using SonOfPicasso.Core.Interfaces;
using SonOfPicasso.Core.Scheduling;
using SonOfPicasso.UI.Interfaces;

namespace SonOfPicasso.UI.ViewModels
{
    public class ApplicationViewModel : ReactiveObject, IApplicationViewModel
    {
        private readonly ILogger _logger;
        private readonly ISchedulerProvider _schedulerProvider;
        private readonly IImageManagementService _imageManagementService;
        private readonly Func<IImageViewModel> _imageViewModelFactory;
        private readonly Func<IImageFolderViewModel> _imageFolderViewModelFactory;

        public ApplicationViewModel(ILogger logger,
            ISchedulerProvider schedulerProvider,
            IImageManagementService imageManagementService,
            Func<IImageViewModel> imageViewModelFactory,
            Func<IImageFolderViewModel> imageFolderViewModelFactory)
        {
            _logger = logger;
            _schedulerProvider = schedulerProvider;
            _imageManagementService = imageManagementService;
            _imageViewModelFactory = imageViewModelFactory;
            _imageFolderViewModelFactory = imageFolderViewModelFactory;

            Images = new ObservableCollection<IImageViewModel>();
            ImageFolders = new ObservableCollection<IImageFolderViewModel>();

            AddFolder = ReactiveCommand.CreateFromObservable<string, Unit>(ExecuteAddFolder);
        }

        public ObservableCollection<IImageViewModel> Images { get; }
        public ObservableCollection<IImageFolderViewModel> ImageFolders { get; }

        public ReactiveCommand<string, Unit> AddFolder { get; }

        public IObservable<Unit> Initialize()
        {
            _logger.Debug("Initializing");
            return Observable.Return(Unit.Default);
        }

        private IObservable<Unit> ExecuteAddFolder(string addPath)
        {
            return Observable.Return(Unit.Default);
        }
    }
}
