using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using DynamicData;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using SonOfPicasso.Core.Interfaces;
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
                var userSettings = await sharedCache.GetUserSettings();

                userSettings.Paths =
                    (userSettings.Paths ?? Enumerable.Empty<string>()).Append(path).Distinct().ToArray();

                await sharedCache.SetUserSettings(userSettings);
            });
        }

        public void Initialize()
        {
            _logger.LogDebug("Initialized");

            _sharedCache.GetUserSettings()
                .ObserveOn(_schedulerProvider.MainThreadScheduler)
                .Subscribe(settings =>
                {
                    Paths.Clear();
                    Paths.AddRange(settings.Paths);
                });
        }

        private ObservableCollection<string> _paths = new ObservableCollection<string>();

        public ObservableCollection<string> Paths
        {
            get => _paths;
            set => this.RaiseAndSetIfChanged(ref _paths, value);
        }
    }
}
