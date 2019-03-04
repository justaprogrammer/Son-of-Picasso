using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using DynamicData;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using SonOfPicasso.Core.Interfaces;
using SonOfPicasso.Core.Scheduling;
using SonOfPicasso.UI.Interfaces;

namespace SonOfPicasso.UI.ViewModels
{
    public class ApplicationViewModel : ReactiveObject, IApplicationViewModel
    {
        private readonly ILogger<ApplicationViewModel> _logger;
        private readonly ISchedulerProvider _schedulerProvider;
        private readonly IImageManagementService _imageManagementService;
        private readonly IServiceProvider _serviceProvider;

        public ApplicationViewModel(ILogger<ApplicationViewModel> logger,
            ISchedulerProvider schedulerProvider,
            IImageManagementService imageManagementService,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _schedulerProvider = schedulerProvider;
            _imageManagementService = imageManagementService;
            _serviceProvider = serviceProvider;

            Images = new ObservableCollection<IImageViewModel>();
            ImageFolders = new ObservableCollection<IImageFolderViewModel>();

            AddFolder = ReactiveCommand.CreateFromObservable<string, Unit>(ExecuteAddFolder);
        }

        public ObservableCollection<IImageViewModel> Images { get; }
        public ObservableCollection<IImageFolderViewModel> ImageFolders { get; }

        public ReactiveCommand<string, Unit> AddFolder { get; }

        public IObservable<Unit> Initialize()
        {
            _logger.LogDebug("Initializing");

            var getImagesObservable = _imageManagementService.GetAllImages()
                .Select(model =>
                {
                    var imageViewModel = _serviceProvider.GetService<IImageViewModel>();
                    imageViewModel.Initialize(model);
                    return imageViewModel;
                })
                .ToArray()
                .ObserveOn(_schedulerProvider.MainThreadScheduler)
                .Select(models =>
                {
                    Images.AddRange(models);
                    return Unit.Default;
                });

            var getImageFoldersObservable = _imageManagementService.GetAllImageFolders()
                .Select(model =>
                {
                    var imageFolderViewModel = _serviceProvider.GetService<IImageFolderViewModel>();
                    imageFolderViewModel.Initialize(model);
                    return imageFolderViewModel;
                })
                .ToArray()
                .ObserveOn(_schedulerProvider.MainThreadScheduler)
                .Select(models =>
                {
                    ImageFolders.AddRange(models);
                    return Unit.Default;
                });

            return Observable.Zip(getImagesObservable, getImageFoldersObservable)
                .Select(_ => Unit.Default);
        }

        private IObservable<Unit> ExecuteAddFolder(string addPath)
        {
            return _imageManagementService.AddFolder(addPath)
                .Select(tuple =>
                {
                    var (imageFolderModel, imageModels) = tuple;
                    var imageFolderViewModel = _serviceProvider.GetService<IImageFolderViewModel>();
                    imageFolderViewModel.Initialize(imageFolderModel);

                    var imageViewModels = imageModels.Select(model =>
                    {
                        var imageViewModel = _serviceProvider.GetService<IImageViewModel>();
                        imageViewModel.Initialize(model);
                        return imageViewModel;
                    }).ToArray();

                    return (imageFolderViewModel, imageViewModels);
                })
                .ObserveOn(_schedulerProvider.MainThreadScheduler)
                .Select(tuple =>
                {
                    var (imageFolderModel, imageModels) = tuple;
                    ImageFolders.Add(imageFolderModel);
                    Images.AddRange(imageModels);
                    return Unit.Default;
                });
        }
    }
}
