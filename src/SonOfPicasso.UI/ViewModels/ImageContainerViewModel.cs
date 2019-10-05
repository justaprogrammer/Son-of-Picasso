using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using DynamicData;
using DynamicData.Binding;
using MoreLinq;
using ReactiveUI;
using SonOfPicasso.Core.Model;
using SonOfPicasso.Core.Scheduling;
using SonOfPicasso.UI.Interfaces;
using SonOfPicasso.UI.ViewModels.Abstract;

namespace SonOfPicasso.UI.ViewModels
{
    public class ImageContainerViewModel : ViewModelBase, IImageContainerViewModel
    {
        private readonly Func<ImageRowViewModel> _imageRowViewModelFactory;
        private readonly ObservableCollectionExtended<ImageRowViewModel> _imageRowViewModels;
        private readonly ISchedulerProvider _schedulerProvider;
        private readonly ObservableAsPropertyHelper<ImageViewModel> _selectedImage;
        private readonly ReplaySubject<ImageViewModel> _selectedImageReplay;
        private readonly ObservableAsPropertyHelper<ImageRowViewModel> _selectedImageRow;
        private readonly ReplaySubject<ImageRowViewModel> _selectedImageRowReplay;

        private ImageContainer _imageContainer;

        public ImageContainerViewModel(
            Func<ImageRowViewModel> imageRowViewModelFactory,
            ViewModelActivator activator
        ) : base(activator)
        {
            _imageRowViewModelFactory = imageRowViewModelFactory;
            _imageRowViewModels = new ObservableCollectionExtended<ImageRowViewModel>();

            _selectedImageRowReplay = new ReplaySubject<ImageRowViewModel>(1);
            _selectedImageRow = _selectedImageRowReplay.ToProperty(this, nameof(SelectedImageRow));

            _selectedImageReplay = new ReplaySubject<ImageViewModel>();
            _selectedImage = _selectedImageReplay.ToProperty(this, nameof(SelectedImage));
        }

        public string Name => _imageContainer.Name;

        public string ContainerId => _imageContainer.Id;

        public ImageContainerTypeEnum ContainerType => _imageContainer.ContainerType;

        public DateTime Date => _imageContainer.Date;

        public IObservableCollection<ImageRowViewModel> ImageRowViewModels => _imageRowViewModels;

        public IApplicationViewModel ApplicationViewModel { get; private set; }

        public ImageRowViewModel SelectedImageRow => _selectedImageRow.Value;

        public ImageViewModel SelectedImage => _selectedImage.Value;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _selectedImage?.Dispose();
                _selectedImageReplay?.Dispose();
                _selectedImageRow?.Dispose();
                _selectedImageRowReplay?.Dispose();
            }

            base.Dispose(disposing);
        }

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
                    .WhenAnyPropertyChanged(nameof(ImageRowViewModel.SelectedImage))
                    .Subscribe(imageRowViewModel =>
                    {
                        var selectedImageChanged = imageRowViewModel.SelectedImage != null
                                                   && imageRowViewModel.SelectedImage != SelectedImage;

                        var selectedRowClearing = imageRowViewModel.SelectedImage == null
                                                  && imageRowViewModel == SelectedImageRow;

                        if (selectedImageChanged
                            || selectedRowClearing)
                        {
                            if (imageRowViewModel.SelectedImage == null)
                            {
                                _selectedImageRowReplay.OnNext(null);
                                _selectedImageReplay.OnNext(null);
                            }
                            else
                            {
                                _selectedImageRowReplay.OnNext(imageRowViewModel);
                                _selectedImageReplay.OnNext(imageRowViewModel?.SelectedImage);
                            }
                        }
                    })
                    .DisposeWith(d);

                sourceList.Connect()
                    .Bind(_imageRowViewModels)
                    .Subscribe()
                    .DisposeWith(d);

                applicationViewModel
                    .WhenPropertyChanged(model => model.SelectedImageContainer, false)
                    .Subscribe(propertyValue =>
                    {
                        var imageContainerViewModel = propertyValue.Value;
                        var selectedContainerIsNull = imageContainerViewModel == null;
                        var selectedContainerIsNotThis = imageContainerViewModel != this;
                        var selectedImageRowIsNotNull =
                            !selectedContainerIsNull && imageContainerViewModel.SelectedImageRow != null;
                        var thisImageRowIsNotNull = SelectedImageRow != null;

                        if (selectedContainerIsNull && thisImageRowIsNotNull
                            || selectedContainerIsNotThis && selectedImageRowIsNotNull && thisImageRowIsNotNull)
                        {
                            _selectedImageRowReplay.OnNext(null);
                            _selectedImageReplay.OnNext(null);
                        }
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
            var imageRefViewModel = _imageRowViewModelFactory();
            imageRefViewModel.Initialize(imageRef, this);
            return imageRefViewModel;
        }
    }
}