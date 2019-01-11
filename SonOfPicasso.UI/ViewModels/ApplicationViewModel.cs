using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using DynamicData;
using Microsoft.Extensions.Logging;
using MoreLinq;
using ReactiveUI;
using SonOfPicasso.Core.Interfaces;
using SonOfPicasso.Core.Models;
using SonOfPicasso.UI.Scheduling;

namespace SonOfPicasso.UI.ViewModels
{
    public class ApplicationViewModel : ReactiveObject, IApplicationViewModel
    {
        private readonly ILogger<ApplicationViewModel> _logger;
        private readonly ISchedulerProvider _schedulerProvider;
        private readonly ISharedCache _sharedCache;
        private readonly IImageLocationService _imageLocationService;

        public ReactiveCommand<string, Unit> AddFolder { get; }

        public ApplicationViewModel(ILogger<ApplicationViewModel> logger,
            ISchedulerProvider schedulerProvider,
            ISharedCache sharedCache,
            IImageLocationService imageLocationService)
        {
            _logger = logger;
            _schedulerProvider = schedulerProvider;
            _sharedCache = sharedCache;
            _imageLocationService = imageLocationService;

            AddFolder = ReactiveCommand.Create<string>(async path =>
            {
                var imageFolders = await sharedCache.GetImageFolders();
                if (!imageFolders.ContainsKey(path))
                {
                    imageFolders.Add(path, new ImageFolder { Path = path });
                }

                await sharedCache.SetImageFolders(imageFolders);

                LoadImageFolders();
            });
        }

        public void Initialize()
        {
            _logger.LogDebug("Initialized");

            LoadImageFolders();
        }

        private void LoadImageFolders()
        {
            _sharedCache.GetImageFolders()
                .ObserveOn(_schedulerProvider.MainThreadScheduler)
                .Subscribe(imageFolders =>
                {
                    ImageFolders.Clear();
                    ImageFolders.AddRange(imageFolders.Values);
                });
        }

        public ObservableCollection<ImageFolder> ImageFolders { get; } = new ObservableCollection<ImageFolder>();
    }
}
