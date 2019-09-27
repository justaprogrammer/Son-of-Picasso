using System;
using System.Reactive;
using System.Reactive.Linq;
using ReactiveUI;
using Serilog;
using SonOfPicasso.Core.Interfaces;
using SonOfPicasso.UI.Interfaces;

namespace SonOfPicasso.UI.ViewModels
{
    class AddAlbumViewModel : ReactiveObject, IAddAlbumViewModel
    {
        private readonly ILogger _logger;
        private readonly IImageManagementService _imageManagementService;

        public AddAlbumViewModel(ViewModelActivator activator, ILogger logger, IImageManagementService imageManagementService)
        {
            _logger = logger;
            _imageManagementService = imageManagementService;
            Activator = activator;

            Continue = ReactiveCommand.CreateFromObservable(OnContinue);
        }

        private IObservable<Unit> OnContinue()
        {
            return Observable.Return(Unit.Default);
        }

        public ViewModelActivator Activator { get; }
        public ReactiveCommand<Unit, Unit> Continue { get; }

        private string _albumName;
     
        public string AlbumName
        {
            get => _albumName;
            set => this.RaiseAndSetIfChanged(ref _albumName, value);
        }
    }
}