﻿using System;
using System.Reactive;
using System.Reactive.Linq;
using ReactiveUI;
using ReactiveUI.Validation.Extensions;
using ReactiveUI.Validation.Helpers;
using Serilog;
using SonOfPicasso.Core.Interfaces;
using SonOfPicasso.Core.Scheduling;

namespace SonOfPicasso.UI.ViewModels
{
    public class AddAlbumViewModel : ReactiveValidationObject<AddAlbumViewModel>
    {
        private readonly IImageManagementService _imageManagementService;
        private readonly ILogger _logger;

        private string _albumName = string.Empty;

        public AddAlbumViewModel(ViewModelActivator activator, ILogger logger,
            IImageManagementService imageManagementService, ISchedulerProvider schedulerProvider) : base(
            schedulerProvider.TaskPool)
        {
            _logger = logger;
            _imageManagementService = imageManagementService;
            Activator = activator;

            var nameValid =
                this.WhenAnyValue(model => model.AlbumName, s => !string.IsNullOrWhiteSpace(s));

            AlbumNameRule = 
                this.ValidationRule(model => nameValid, (model, b) => "Album name must be set");

            Continue = ReactiveCommand.CreateFromObservable(OnContinue, this.IsValid());

            var observable = Observable.Return(false);
            _displayAlbumNameError = observable.ToProperty(this, model => model.DisplayAlbumNameError, deferSubscription: true)
            this.ValidationContext.Valid.Subscribe(b =>
            {
                ;
            });
            
            this.ValidationContext.ValidationStatusChange.Subscribe(b =>
            {
                ;
            });
        }

        public ViewModelActivator Activator { get; }

        public ValidationHelper AlbumNameRule { get; }

        readonly ObservableAsPropertyHelper<string> _displayAlbumNameError;
        public string DisplayAlbumNameError => _displayAlbumNameError.Value;

        public string AlbumName
        {
            get => _albumName;
            set => this.RaiseAndSetIfChanged(ref _albumName, value);
        }

        public ReactiveCommand<Unit, Unit> Continue { get; }

        private IObservable<Unit> OnContinue()
        {
            return Observable.Return(Unit.Default);
        }
    }
}