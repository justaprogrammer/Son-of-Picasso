using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using DynamicData;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using SonOfPicasso.Core.Interfaces;
using SonOfPicasso.Core.Models;
using SonOfPicasso.Core.Scheduling;
using SonOfPicasso.UI.Interfaces;

namespace SonOfPicasso.UI.ViewModels
{
    public class ApplicationViewModel : ReactiveObject, IApplicationViewModel
    {
        private readonly ILogger<ApplicationViewModel> _logger;
        private readonly ISchedulerProvider _schedulerProvider;
        private readonly ISharedCache _sharedCache;
        private readonly IImageLocationService _imageLocationService;
        private readonly IServiceProvider _serviceProvider;

        public ApplicationViewModel(ILogger<ApplicationViewModel> logger,
            ISchedulerProvider schedulerProvider,
            ISharedCache sharedCache,
            IImageLocationService imageLocationService,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _schedulerProvider = schedulerProvider;
            _sharedCache = sharedCache;
            _imageLocationService = imageLocationService;
            _serviceProvider = serviceProvider;

            var imageFolders = new ObservableCollection<IImageFolderViewModel>();
            imageFolders.CollectionChanged += ImageFoldersOnCollectionChanged;
            ImageFolders = imageFolders;

            Images = new ObservableCollection<IImageViewModel>();

            AddFolder = ReactiveCommand.CreateFromObservable<string, Unit>(ExecuteAddFolder);
        }

        private void ImageFoldersOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            _logger.LogDebug("ImageFoldersOnCollectionChanged");

            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                var paths = e.NewItems.Cast<ImageFolderViewModel>()
                    .Select(folder => folder.ImageFolder.Path);

                ScanFolders(paths).Subscribe();
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {

            }
            else if (e.Action == NotifyCollectionChangedAction.Reset)
            {

            }
        }

        public ObservableCollection<IImageFolderViewModel> ImageFolders { get; }

        public ObservableCollection<IImageViewModel> Images { get; }

        public ReactiveCommand<string, Unit> AddFolder { get; }

        public IObservable<Unit> Initialize()
        {
            _logger.LogDebug("Initializing");

            return LoadImageFolders()
                .Select(unit =>
                {
                    _logger.LogDebug("Initialized");
                    return unit;
                });
        }

        private IObservable<Unit> ExecuteAddFolder(string path)
        {
            return _sharedCache.GetImageFolders()
                .ObserveOn(_schedulerProvider.TaskPool)
                .SelectMany(imageFolders =>
                {
                    if (!imageFolders.ContainsKey(path))
                    {
                        imageFolders.Add(path, new ImageFolder { Path = path });
                    }

                    return _sharedCache.SetImageFolders(imageFolders);
                })
                .SelectMany(_ => LoadImageFolders());
        }

        private IObservable<Unit> ScanFolders(IEnumerable<string> paths)
        {
            return paths.ToObservable()
                .SelectMany(s => _imageLocationService.GetImages(s))
                .SelectMany(fileInfo => fileInfo)
                .Select(fileInfo =>
                {
                    var image = new Image { Path = fileInfo.FullName };

                    var imageViewModel = _serviceProvider.GetService<IImageViewModel>();
                    imageViewModel.Initialize(image);

                    return imageViewModel;
                })
                .ToArray()
                .ObserveOn(_schedulerProvider.MainThreadScheduler)
                .Select(items =>
                {
                    Images.AddRange(items);
                    return Unit.Default;
                });
        }

        private IObservable<Unit> LoadImageFolders()
        {
            _logger.LogDebug("LoadImageFolders");

            return _sharedCache.GetImageFolders()
                .ObserveOn(_schedulerProvider.MainThreadScheduler)
                .Select(imageFolders =>
                {
                    ImageFolders.Clear();
                    var enumerable = imageFolders.Values.Select(imageFolder =>
                    {
                        var imageFolderViewModel = _serviceProvider.GetService<IImageFolderViewModel>();
                        imageFolderViewModel.Initialize(imageFolder);

                        return imageFolderViewModel;
                    });

                    ImageFolders.AddRange(enumerable);

                    return Unit.Default;
                });
        }
    }
}
