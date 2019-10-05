using System;
using System.Collections.Generic;
using System.Linq;
using ReactiveUI;
using SonOfPicasso.Core.Model;
using SonOfPicasso.UI.Interfaces;
using SonOfPicasso.UI.ViewModels.Abstract;

namespace SonOfPicasso.UI.ViewModels
{
    public class ImageRowViewModel : ViewModelBase, IImageRowViewModel
    {
        private readonly Func<ImageViewModel> _imageRefViewModelFactory;

        public ImageRowViewModel(Func<ImageViewModel> imageRefViewModelFactory, ViewModelActivator activator) :
            base(activator)
        {
            _imageRefViewModelFactory = imageRefViewModelFactory;
        }

        public IImageContainerViewModel ImageContainerViewModel { get; private set; }

        public IList<ImageViewModel> ImageViewModels { get; private set; }

        public void Initialize(IEnumerable<ImageRef> imageRefs, IImageContainerViewModel imageContainerViewModel)
        {
            if (imageRefs == null) throw new ArgumentNullException(nameof(imageRefs));
            ImageContainerViewModel = imageContainerViewModel ??
                                      throw new ArgumentNullException(nameof(imageContainerViewModel));

            ImageViewModels = imageRefs.Select(CreateImageRefViewModel).ToArray();
        }

        private ImageViewModel CreateImageRefViewModel(ImageRef imageRef)
        {
            var imageRefViewModel = _imageRefViewModelFactory();
            imageRefViewModel.Initialize(imageRef, this);
            return imageRefViewModel;
        }
    }
}