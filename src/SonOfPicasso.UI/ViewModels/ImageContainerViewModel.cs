using System;
using System.Collections.Generic;
using System.Linq;
using DynamicData.Binding;
using MoreLinq;
using ReactiveUI;
using SonOfPicasso.Core.Model;
using SonOfPicasso.UI.ViewModels.Abstract;

namespace SonOfPicasso.UI.ViewModels
{
    public class ImageContainerViewModel : ViewModelBase
    {
        private readonly Func<ImageRefRowViewModel> _imageRefRowViewModelFactory;
        private ImageContainer _imageContainer;

        public ImageContainerViewModel(
            Func<ImageRefRowViewModel> imageRefRowViewModelFactory,
            ViewModelActivator activator
        ) : base(activator)
        {
            _imageRefRowViewModelFactory = imageRefRowViewModelFactory;
        }

        public bool IsExpanded => true;

        public string Name => _imageContainer.Name;

        public string ContainerId => _imageContainer.Id;

        public ImageContainerTypeEnum ContainerType => _imageContainer.ContainerType;

        public DateTime Date => _imageContainer.Date;

        public IList<ImageRefRowViewModel> ImageRefRows { get; private set; }

        public void Initialize(ImageContainer imageContainer)
        {
            _imageContainer = imageContainer ?? throw new ArgumentNullException(nameof(imageContainer));

            ImageRefRows = imageContainer.ImageRefs
                    .OrderBy(imageRef => imageRef.Date)
                    .Batch(3)
                    .Select(CreateImageRefViewModel)
                    .ToArray();
        }

        private ImageRefRowViewModel CreateImageRefViewModel(IEnumerable<ImageRef> imageRef)
        {
            var imageRefViewModel = _imageRefRowViewModelFactory();
            imageRefViewModel.Initialize(imageRef);
            return imageRefViewModel;
        }
    }
}