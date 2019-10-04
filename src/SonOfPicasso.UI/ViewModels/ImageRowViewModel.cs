using System;
using System.Collections.Generic;
using System.Linq;
using ReactiveUI;
using SonOfPicasso.Core.Model;
using SonOfPicasso.UI.ViewModels.Abstract;

namespace SonOfPicasso.UI.ViewModels
{
    public class ImageRowViewModel : ViewModelBase
    {
        private readonly Func<ImageViewModel> _imageRefViewModelFactory;
        private ImageViewModel selectedItem;

        public ImageRowViewModel(Func<ImageViewModel> imageRefViewModelFactory, ViewModelActivator activator) :
            base(activator)
        {
            _imageRefViewModelFactory = imageRefViewModelFactory;
        }

        public IList<ImageViewModel> ImageRefViewModels { get; private set; }

        public ImageViewModel SelectedItem
        {
            get => selectedItem;
            set => this.RaiseAndSetIfChanged(ref selectedItem, value);
        }

        public void Initialize(IEnumerable<ImageRef> imageRefs)
        {
            if (imageRefs == null) throw new ArgumentNullException(nameof(imageRefs));

            ImageRefViewModels = imageRefs.Select(CreateImageRefViewModel).ToArray();
        }

        private ImageViewModel CreateImageRefViewModel(ImageRef imageRef)
        {
            var imageRefViewModel = _imageRefViewModelFactory();
            imageRefViewModel.Initialize(imageRef);
            return imageRefViewModel;
        }
    }
}