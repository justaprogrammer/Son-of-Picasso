using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
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
        private readonly IDataCache _dataCache;
        private readonly IImageLocationService _imageLocationService;
        private readonly IServiceProvider _serviceProvider;

        public ApplicationViewModel(ILogger<ApplicationViewModel> logger,
            ISchedulerProvider schedulerProvider,
            IDataCache dataCache,
            IImageLocationService imageLocationService,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _schedulerProvider = schedulerProvider;
            _dataCache = dataCache;
            _imageLocationService = imageLocationService;
            _serviceProvider = serviceProvider;

            ImageFolders = new ObservableCollection<IImageFolderViewModel>();

            AddFolder = ReactiveCommand.CreateFromObservable<string, Unit>(ExecuteAddFolder);
        }

        public ObservableCollection<IImageFolderViewModel> ImageFolders { get; }

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

        private IObservable<Unit> ExecuteAddFolder(string addPath)
        {
            return _dataCache.GetFolderList()
                .ObserveOn(_schedulerProvider.TaskPool)
                .SelectMany(paths =>
                {
                    if (!paths.Any(path => path.Equals(addPath)))
                    {
                        paths = paths.Append(addPath).ToArray();
                    }

                    return _dataCache.SetFolderList(paths);
                })
                .SelectMany(_ => LoadImageFolders());
        }

//        private IObservable<Unit> ScanFolders(IEnumerable<string> paths)
//        {
//            return paths.ToObservable()
//                .SelectMany(s => _imageLocationService.GetImages(s))
//                .SelectMany(fileInfo => fileInfo)
//                .Select(fileInfo =>
//                {
//                    var image = new Image { Path = fileInfo.FullName };
//
//                    var imageViewModel = _serviceProvider.GetService<IImageViewModel>();
//                    imageViewModel.Initialize(image);
//
//                    return imageViewModel;
//                })
//                .ToArray()
//                .ObserveOn(_schedulerProvider.MainThreadScheduler)
//                .Select(items =>
//                {
//                    Images.AddRange(items);
//                    return Unit.Default;
//                });
//        }

        private IObservable<Unit> LoadImageFolders()
        {
            _logger.LogDebug("LoadImageFolders");

            return _dataCache.GetFolderList()
                .ObserveOn(_schedulerProvider.MainThreadScheduler)
                .Select(imageFolders =>
                {
                    ImageFolders.Clear();
                    var enumerable = imageFolders.Select(path =>
                    {
                        var imageFolderViewModel = _serviceProvider.GetService<IImageFolderViewModel>();
                        imageFolderViewModel.Initialize(path);

                        return imageFolderViewModel;
                    });

                    ImageFolders.AddRange(enumerable);

                    return Unit.Default;
                });
        }
    }
}
