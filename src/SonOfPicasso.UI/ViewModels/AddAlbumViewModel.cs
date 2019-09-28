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

        public AddAlbumViewModel(ViewModelActivator activator, ILogger logger,
            IImageManagementService imageManagementService, ISchedulerProvider schedulerProvider) : base(
            schedulerProvider.TaskPool)
        {
            _logger = logger;
            _imageManagementService = imageManagementService;
            Activator = activator;
            
            AlbumNameRule =
                this.ValidationRule(model => model.AlbumName, 
                    s => !string.IsNullOrWhiteSpace(s),
                    "Album name must be set");

            _displayAlbumNameError = OnValidationHelperChange(model => model.AlbumName, model => model.AlbumNameRule.IsValid)
                .Do(b => { ; })
                .ToProperty(this, model => model.DisplayAlbumNameError);
            
            Continue = ReactiveCommand.CreateFromObservable(OnContinue, this.IsValid());

            Cancel = ReactiveCommand.Create(() => Unit.Default);
        }

        public ViewModelActivator Activator { get; }

        public ValidationHelper AlbumNameRule { get; }

        public string AlbumName
        {
            get => _albumName;
            set => this.RaiseAndSetIfChanged(ref _albumName, value);
        }

        private readonly ObservableAsPropertyHelper<bool> _displayAlbumNameError;
        public bool DisplayAlbumNameError => _displayAlbumNameError.Value;

        public ReactiveCommand<Unit, Unit> Continue { get; }
        public ReactiveCommand<Unit, Unit> Cancel { get; }

        private IObservable<bool> OnValidationHelperChange<T>(Expression<Func<AddAlbumViewModel, T>> modelPropertyExpression,
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