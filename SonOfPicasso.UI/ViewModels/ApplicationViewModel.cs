using System.Reactive;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using SonOfPicasso.Core.Interfaces;

namespace SonOfPicasso.UI.ViewModels
{
    public class ApplicationViewModel : ReactiveObject, IApplicationViewModel
    {
        public ILogger<ApplicationViewModel> Logger { get; }
        public ISharedCache SharedCache { get; }
        public IImageLocationService ImageLocationService { get; }
        public ReactiveCommand<Unit, Unit> BrowseToDatabase { get; }

        public ApplicationViewModel(ILogger<ApplicationViewModel> logger,
            ISharedCache sharedCache,
            IImageLocationService imageLocationService)
        {
            Logger = logger;
            SharedCache = sharedCache;
            ImageLocationService = imageLocationService;

            BrowseToDatabase = ReactiveCommand.Create(() => { });
        }

        public void Initialize()
        {
            Logger.LogDebug("Initialized");
        }

        private string _pathToDatabase;

        public string PathToImages
        {
            get => _pathToDatabase;
            set => this.RaiseAndSetIfChanged(ref _pathToDatabase, value);
        }
    }
}
