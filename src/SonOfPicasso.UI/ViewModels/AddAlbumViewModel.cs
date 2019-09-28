using System;
using System.Linq.Expressions;
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
        private bool _displayAlbumNameError;

        public AddAlbumViewModel(ViewModelActivator activator, ILogger logger,
            IImageManagementService imageManagementService, ISchedulerProvider schedulerProvider) : base(
            schedulerProvider.TaskPool)
        {
            _logger = logger;
            _imageManagementService = imageManagementService;
            Activator = activator;

            var nameValid =
                this.WhenAnyValue<AddAlbumViewModel, bool, string>(model => model.AlbumName,
                    s => !string.IsNullOrWhiteSpace(s));

            AlbumNameRule =
                this.ValidationRule(model => nameValid, (model, b) => "Album name must be set");

            Continue = ReactiveCommand.CreateFromObservable(OnContinue, this.IsValid());

            GetObservable(model => model.AlbumName, model => model.AlbumNameRule.IsValid)
                .Subscribe(b =>
                {
                    ;
                });


            ValidationContext.Valid.Subscribe(b =>
            {
                ;
            });

            ValidationContext.ValidationStatusChange.Subscribe(b =>
            {
                ;
            });
        }

        public ViewModelActivator Activator { get; }

        public ValidationHelper AlbumNameRule { get; }

        public string AlbumName
        {
            get => _albumName;
            set => this.RaiseAndSetIfChanged(ref _albumName, value);
        }

        public bool DisplayAlbumNameError
        {
            get => _displayAlbumNameError;
            set => this.RaiseAndSetIfChanged(ref _displayAlbumNameError, value);
        }

        public ReactiveCommand<Unit, Unit> Continue { get; }

        private IObservable<bool> GetObservable<T>(Expression<Func<AddAlbumViewModel, T>> modelPropertyExpression,
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