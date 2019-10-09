using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using DynamicData;
using DynamicData.Binding;
using MoreLinq;
using ReactiveUI;
using SonOfPicasso.Core.Model;
using SonOfPicasso.Core.Scheduling;
using SonOfPicasso.UI.ViewModels.Abstract;

namespace SonOfPicasso.UI.ViewModels
{
    public class ImageContainerViewModel : ViewModelBase, IDisposable
    {
        private readonly ObservableCollectionExtended<ImageViewModel> _imageViewModels;
        private readonly Func<ImageViewModel> _imageViewModelFactory;
        private readonly ISchedulerProvider _schedulerProvider;

        private ImageContainer _imageContainer;

        public ImageContainerViewModel(
            Func<ImageViewModel> imageViewModelFactory,
            ViewModelActivator activator, 
            ISchedulerProvider schedulerProvider) : base(activator)
        {
            _imageViewModelFactory = imageViewModelFactory;
            _schedulerProvider = schedulerProvider;
            _imageViewModels = new ObservableCollectionExtended<ImageViewModel>();
        }

        public string Name => _imageContainer.Name;

        public string ContainerId => _imageContainer.Id;

        public ImageContainerTypeEnum ContainerType => _imageContainer.ContainerType;

        public DateTime Date => _imageContainer.Date;

        public IObservableCollection<ImageViewModel> ImageViewModels => _imageViewModels;

        public ApplicationViewModel ApplicationViewModel { get; private set; }

        public void Initialize(ImageContainer imageContainer, ApplicationViewModel applicationViewModel)
        {
            _imageContainer = imageContainer ?? throw new ArgumentNullException(nameof(imageContainer));
            ApplicationViewModel =
                applicationViewModel ?? throw new ArgumentNullException(nameof(applicationViewModel));

            this.WhenActivated((CompositeDisposable d) =>
            {
                var sourceCache = new SourceCache<ImageRef, string>(refs => refs.Id)
                    .DisposeWith(d);

                sourceCache.Connect()
                    .Transform(CreateImageViewModel);

                sourceCache.Edit(updater => updater.Load(imageContainer.ImageRefs
                    .OrderBy(imageRef => imageRef.Date)));
            });
        }

        public void Dispose()
        {
            ApplicationViewModel?.Dispose();
        }

        private ImageViewModel CreateImageViewModel(ImageRef imageRef)
        {
            var imageRefViewModel = _imageViewModelFactory();
            imageRefViewModel.Initialize(imageRef);
            return imageRefViewModel;
        }
    }
}