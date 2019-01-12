using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using DynamicData;
using Microsoft.Extensions.Logging;
using MoreLinq;
using ReactiveUI;
using SonOfPicasso.Core.Interfaces;
using SonOfPicasso.Core.Models;
using SonOfPicasso.Core.Scheduling;
using SonOfPicasso.UI.Scheduling;

namespace SonOfPicasso.UI.ViewModels
{
    public class ApplicationViewModel : ReactiveObject, IApplicationViewModel
    {
        private readonly ILogger<ApplicationViewModel> _logger;
        private readonly ISchedulerProvider _schedulerProvider;
        private readonly ISharedCache _sharedCache;
        private readonly IImageLocationService _imageLocationService;

        public ApplicationViewModel(ILogger<ApplicationViewModel> logger,
            ISchedulerProvider schedulerProvider,
            ISharedCache sharedCache,
            IImageLocationService imageLocationService)
        {
            _logger = logger;
            _schedulerProvider = schedulerProvider;
            _sharedCache = sharedCache;
            _imageLocationService = imageLocationService;

            var imageFolders = new ObservableCollection<ImageFolder>();
            imageFolders.CollectionChanged += ImageFoldersOnCollectionChanged;
            ImageFolders = imageFolders;

            Images = new ObservableCollection<Image>();

            AddFolder = ReactiveCommand.CreateFromObservable<string, Unit>(ExecuteAddFolder);
        }

        private void ImageFoldersOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            _logger.LogDebug("ImageFoldersOnCollectionChanged");
        }

        public ObservableCollection<ImageFolder> ImageFolders { get; }

        public ObservableCollection<Image> Images { get; }

        public ReactiveCommand<string, Unit> AddFolder { get; }

        public IObservable<Unit> Initialize()
        {
            _logger.LogDebug("Initializing");

            return LoadImageFolders()
                .ObserveOn(_schedulerProvider.TaskPool)
                .SelectMany(unit => {
                    return ImageFolders
                        .ToObservable()
                        .ObserveOn(_schedulerProvider.TaskPool)
                        .SelectMany(folder => ScanFolder(folder.Path))
                        .Append(Unit.Default)
                        .LastAsync();
                })
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

        private IObservable<Unit> ScanFolder(string path)
        {
            return _imageLocationService.GetImages(path)
                .ObserveOn(_schedulerProvider.MainThreadScheduler)
                .Select(fileInfos =>
                {
                    Images.AddRange(fileInfos.Select(fileInfo => new Image { Path = fileInfo.FullName }));
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
                    ImageFolders.AddRange(imageFolders.Values);

                    return Unit.Default;
                });
        }
    }
}
