using System;
using System.Collections.Generic;
using System.Linq;
using ReactiveUI;
using SonOfPicasso.Core.Model;
using SonOfPicasso.UI.ViewModels.Abstract;

namespace SonOfPicasso.UI.ViewModels
{
    public class ImageRefRowViewModel : ViewModelBase
    {
        private readonly Func<ImageRefViewModel> _imageRefViewModelFactory;

        public ImageRefRowViewModel(Func<ImageRefViewModel> imageRefViewModelFactory, ViewModelActivator activator) :
            base(activator)
        {
            _imageRefViewModelFactory = imageRefViewModelFactory;
        }

        public IList<ImageRefViewModel> ImageRefViewModels { get; private set; }

        public void Initialize(IEnumerable<ImageRef> imageRefs)
        {
            if (imageRefs == null) throw new ArgumentNullException(nameof(imageRefs));

            ImageRefViewModels = imageRefs.Select(CreateImageRefViewModel).ToArray();
        }

        private ImageRefViewModel CreateImageRefViewModel(ImageRef imageRef)
        {
            var imageRefViewModel = _imageRefViewModelFactory();
            imageRefViewModel.Initialize(imageRef);
            return imageRefViewModel;
        }
    }
}