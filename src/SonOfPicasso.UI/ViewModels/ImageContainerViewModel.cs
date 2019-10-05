using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using DynamicData;
using DynamicData.Binding;
using MoreLinq;
using ReactiveUI;
using SonOfPicasso.Core.Model;
using SonOfPicasso.UI.Interfaces;
using SonOfPicasso.UI.ViewModels.Abstract;

namespace SonOfPicasso.UI.ViewModels
{
    public class ImageContainerViewModel : ViewModelBase, IImageContainerViewModel
    {
        private readonly Func<ImageRowViewModel> _imageRowViewModelFactory;
        private readonly ObservableCollectionExtended<ImageRowViewModel> _imageRowViewModels;

        private ImageContainer _imageContainer;

        public ImageContainerViewModel(
            Func<ImageRowViewModel> imageRowViewModelFactory,
            ViewModelActivator activator
        ) : base(activator)
        {
            _imageRowViewModelFactory = imageRowViewModelFactory;
            _imageRowViewModels = new ObservableCollectionExtended<ImageRowViewModel>();
            ImageRowViewModels = _imageRowViewModels;
        }

        public string Name => _imageContainer.Name;

        public string ContainerId => _imageContainer.Id;

        public ImageContainerTypeEnum ContainerType => _imageContainer.ContainerType;

        public DateTime Date => _imageContainer.Date;

        public IObservableCollection<ImageRowViewModel> ImageRowViewModels { get; }

        public IApplicationViewModel ApplicationViewModel { get; private set; }

        public void Initialize(ImageContainer imageContainer, IApplicationViewModel applicationViewModel)
        {
            _imageContainer = imageContainer ?? throw new ArgumentNullException(nameof(imageContainer));
            ApplicationViewModel =
                applicationViewModel ?? throw new ArgumentNullException(nameof(applicationViewModel));

            this.WhenActivated(d =>
            {
                var sourceList = new SourceList<ImageRowViewModel>()
                    .DisposeWith(d);

                sourceList.Connect()
                    .Bind(_imageRowViewModels)
                    .Subscribe()
                    .DisposeWith(d);

                sourceList.AddRange(imageContainer.ImageRefs
                    .OrderBy(imageRef => imageRef.Date)
                    .Batch(3)
                    .Select(CreateImageRefViewModel));
            });
        }

        private ImageRowViewModel CreateImageRefViewModel(IEnumerable<ImageRef> imageRef)
        {
            var imageRefViewModel = _imageRowViewModelFactory();
            imageRefViewModel.Initialize(imageRef, this);
            return imageRefViewModel;
        }
    }
}