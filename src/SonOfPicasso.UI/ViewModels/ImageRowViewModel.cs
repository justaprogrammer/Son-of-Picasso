using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using DynamicData.Binding;
using ReactiveUI;
using SonOfPicasso.Core.Model;
using SonOfPicasso.UI.ViewModels.Abstract;

namespace SonOfPicasso.UI.ViewModels
{
    public class ImageRowViewModel : ViewModelBase
    {
        private readonly Func<ImageViewModel> _imageViewModelFactory;
        private ImageViewModel _selectedImage;

        public ImageRowViewModel(Func<ImageViewModel> imageViewModelFactory, ViewModelActivator activator) :
            base(activator)
        {
            _imageViewModelFactory = imageViewModelFactory;
        }

        public ImageViewModel SelectedImage
        {
            get => _selectedImage;
            set => this.RaiseAndSetIfChanged(ref _selectedImage, value);
        }

        public HashSet<string> ImageIdSet { get; private set; }

        public ImageContainerViewModel ImageContainerViewModel { get; private set; }

        public IList<ImageViewModel> ImageViewModels { get; private set; }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _selectedImage?.Dispose();

            base.Dispose(disposing);
        }

        public void Initialize(IEnumerable<ImageRef> imageRefs, ImageContainerViewModel imageContainerViewModel)
        {
            if (imageRefs == null) throw new ArgumentNullException(nameof(imageRefs));
            ImageContainerViewModel = imageContainerViewModel ??
                                      throw new ArgumentNullException(nameof(imageContainerViewModel));

            var imageRefArray = imageRefs.ToArray();
            ImageViewModels = imageRefArray.Select(CreateImageRefViewModel).ToArray();
            ImageIdSet = imageRefArray.Select(imageRef => imageRef.Id).ToHashSet();

            this.WhenActivated(d =>
            {
                imageContainerViewModel
                    .WhenPropertyChanged(model => model.SelectedImageRow, false)
                    .Subscribe(propertyValue =>
                    {
                        var imageRowViewModel = propertyValue.Value;
                        var selectedRowIsNull = imageRowViewModel == null;
                        var selectedRowIsNotThis = imageRowViewModel != this;
                        var selectedImageIsNotNull = !selectedRowIsNull && imageRowViewModel.SelectedImage != null;
                        var thisImageIsNotNull = SelectedImage != null;

                        if (selectedRowIsNull && thisImageIsNotNull
                            || selectedRowIsNotThis && selectedImageIsNotNull && thisImageIsNotNull)
                            SelectedImage = null;
                    })
                    .DisposeWith(d);
            });
        }

        private ImageViewModel CreateImageRefViewModel(ImageRef imageRef)
        {
            var imageRefViewModel = _imageViewModelFactory();
            imageRefViewModel.Initialize(imageRef, this);
            return imageRefViewModel;
        }
    }
}