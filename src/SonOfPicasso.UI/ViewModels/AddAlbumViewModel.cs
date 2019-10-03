﻿using System;
using System.Linq.Expressions;
using System.Reactive;
using System.Reactive.Linq;
using ReactiveUI;
using ReactiveUI.Validation.Extensions;
using ReactiveUI.Validation.Helpers;
using Serilog;
using SonOfPicasso.Core.Interfaces;
using SonOfPicasso.Core.Scheduling;
using SonOfPicasso.UI.ViewModels.Abstract;

namespace SonOfPicasso.UI.ViewModels
{
    public class AddAlbumViewModel : ValidatedViewModelBase<AddAlbumViewModel>, ICreateAlbum
    {
        private readonly ObservableAsPropertyHelper<bool> _displayAlbumNameError;
        private readonly IImageManagementService _imageManagementService;
        private readonly ISchedulerProvider _schedulerProvider;
        private readonly ILogger _logger;

        private string _albumName = string.Empty;
        private DateTime _albumDate = DateTime.Today;

        public AddAlbumViewModel(ViewModelActivator activator, ILogger logger,
            IImageManagementService imageManagementService, ISchedulerProvider schedulerProvider) : base(
            activator, schedulerProvider.TaskPool)
        {
            _logger = logger;
            _imageManagementService = imageManagementService;
            _schedulerProvider = schedulerProvider;

            AlbumNameRule =
                this.ValidationRule(model => model.AlbumName,
                    s => !string.IsNullOrWhiteSpace(s),
                    "Album name must be set");

            _displayAlbumNameError =
                OnValidationHelperChange(model => model.AlbumName, model => model.AlbumNameRule.IsValid)
                    .ToProperty(this, model => model.DisplayAlbumNameError);

            Continue = ReactiveCommand.CreateFromObservable(ExecuteContinue, this.IsValid());
            ContinueInteraction = new Interaction<Unit, Unit>();

            Cancel = ReactiveCommand.CreateFromObservable(ExecuteCancel);
            CancelInteraction = new Interaction<Unit, Unit>();
        }

        public ValidationHelper AlbumNameRule { get; }

        public string AlbumName
        {
            get => _albumName;
            set => this.RaiseAndSetIfChanged(ref _albumName, value);
        }

        public DateTime AlbumDate
        {
            get => _albumDate;
            set => this.RaiseAndSetIfChanged(ref _albumDate, value);
        }

        public bool DisplayAlbumNameError => _displayAlbumNameError.Value;

        public Interaction<Unit, Unit> ContinueInteraction { get; set; }

        public ReactiveCommand<Unit, Unit> Continue { get; }

        public Interaction<Unit, Unit> CancelInteraction { get; set; }

        public ReactiveCommand<Unit, Unit> Cancel { get; }

        private IObservable<Unit> ExecuteCancel()
        {
            return CancelInteraction.Handle(Unit.Default)
                .SubscribeOn(_schedulerProvider.TaskPool)
                .Select(unit => unit);
        }

        private IObservable<Unit> ExecuteContinue()
        {
            return ContinueInteraction.Handle(Unit.Default)
                .SubscribeOn(_schedulerProvider.TaskPool)
                .Select(unit => unit);
        }

        private IObservable<bool> OnValidationHelperChange<T>(
            Expression<Func<AddAlbumViewModel, T>> modelPropertyExpression,
            Expression<Func<AddAlbumViewModel, bool>> validationHelperIsValidExpression)
        {
            var modelPropertyHasChanged = Observable.Return(false)
                .Concat(this.WhenAnyValue(modelPropertyExpression)
                    .Skip(1)
                    .Select(s => true)
                    .Repeat());

            var whenAnyValue = this.WhenAnyValue(validationHelperIsValidExpression);

            return modelPropertyHasChanged
                .CombineLatest(whenAnyValue, (hasChanged, isValid) => hasChanged && !isValid)
                .DistinctUntilChanged();
        }

        private IObservable<Unit> OnContinue()
        {
            return Observable.Return(Unit.Default);
        }
    }
}