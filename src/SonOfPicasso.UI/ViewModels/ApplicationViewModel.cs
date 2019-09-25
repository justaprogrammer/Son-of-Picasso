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

            return LoadData();
        }

        private IObservable<Unit> LoadData()
        {
            var getAllDirectories = _imageManagementService.GetAllDirectoriesWithImages();

            var d1 = getAllDirectories
                .Select(directory =>
                {
                    var imageFolderViewModel = _imageFolderViewModelFactory();
                    imageFolderViewModel.Initialize(directory);
                    return imageFolderViewModel;
                })
                .ToArray()
                .ObserveOn(_schedulerProvider.MainThreadScheduler)
                .Select(models =>
                {
                    ImageFolders.AddRange(models);
                    return Unit.Default;
                });

            var d2 = getAllDirectories
                .Select(directory => directory.Images)
                .SelectMany(observable => observable)
                .Select(image =>
                {
                    var imageViewModel = _imageViewModelFactory();
                    imageViewModel.Initialize(image);
                    return imageViewModel;
                })
                .ToArray()
                .ObserveOn(_schedulerProvider.MainThreadScheduler)
                .Select(models =>
                {
                    Images.AddRange(models);
                    return Unit.Default;
                });

            return Observable.Zip(d1, d2)
                .Select(list => Unit.Default);
        }

        private IObservable<Unit> ExecuteAddFolder(string addPath)
        {
            return _imageManagementService.ScanFolder(addPath)
                .ObserveOn(_schedulerProvider.MainThreadScheduler)
                .Select(images =>
                {
                    Images.Clear();
                    ImageFolders.Clear();
                    return LoadData();
                })
                .SelectMany(observable => observable);
        }
    }
}
