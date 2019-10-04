using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using MoreLinq;
using ReactiveUI;
using SonOfPicasso.Core.Model;
using SonOfPicasso.UI.ViewModels.Abstract;

namespace SonOfPicasso.UI.ViewModels
{
    public class ImageContainerViewModel : ViewModelBase
    {
        private readonly Func<ImageRowViewModel> _imageRefRowViewModelFactory;
        private readonly ObservableCollectionExtended<ImageRowViewModel> _imageRowViewModels;
        
        private ImageContainer _imageContainer;

        public ImageContainerViewModel(
            Func<ImageRowViewModel> imageRefRowViewModelFactory,
            ViewModelActivator activator
        ) : base(activator)
        {
            _imageRefRowViewModelFactory = imageRefRowViewModelFactory;
            _imageRowViewModels = new ObservableCollectionExtended<ImageRowViewModel>();
            ImageRowViewModels = _imageRowViewModels;
        }

        public bool IsExpanded => true;

        public string Name => _imageContainer.Name;

        public string ContainerId => _imageContainer.Id;

        public ImageContainerTypeEnum ContainerType => _imageContainer.ContainerType;

        public DateTime Date => _imageContainer.Date;

        public IObservableCollection<ImageRowViewModel> ImageRowViewModels { get; private set; }

        public void Initialize(ImageContainer imageContainer)
        {
            _imageContainer = imageContainer ?? throw new ArgumentNullException(nameof(imageContainer));

            this.WhenActivated(d =>
            {
                var sourceList = new SourceList<ImageRowViewModel>()
                    .DisposeWith(d);

                sourceList.Connect()
                    .Bind(_imageRowViewModels)
                    .Subscribe()
                    .DisposeWith(d);

                sourceList.Connect()
                    .WhenAnyPropertyChanged("SelectedItem")
                    .Subscribe(model =>
                    {
                        ;
                    })
                    .DisposeWith(d);

                sourceList.AddRange(imageContainer.ImageRefs
                    .OrderBy(imageRef => imageRef.Date)
                    .Batch(3)
                    .Select(CreateImageRefViewModel));    
            });
        }

        private ImageRowViewModel CreateImageRefViewModel(IEnumerable<ImageRef> imageRef)
        {
            var imageRefViewModel = _imageRefRowViewModelFactory();
            imageRefViewModel.Initialize(imageRef);
            return imageRefViewModel;
        }
    }
}