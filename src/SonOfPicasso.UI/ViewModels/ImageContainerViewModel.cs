using System;
using System.Collections.Generic;
using SonOfPicasso.Core.Model;
using SonOfPicasso.UI.ViewModels.Abstract;

namespace SonOfPicasso.UI.ViewModels
{
    public class ImageContainerViewModel : ViewModelBase
    {
        private readonly IImageContainer _imageContainer;

        public ImageContainerViewModel(IImageContainer imageContainer)
        {
            _imageContainer =
                imageContainer ?? throw new ArgumentNullException(nameof(imageContainer));
        }

        public string Name => _imageContainer.Name;

        public string ContainerKey => _imageContainer.Key;

        public ImageContainerTypeEnum ContainerType => _imageContainer.ContainerType;
        public int ContainerTypeId => _imageContainer.Id;

        public IList<ImageRef> ImageRefs => _imageContainer.ImageRefs;

        public int Count => ImageRefs.Count;

        public int Year => _imageContainer.Year;

        public DateTime Date => _imageContainer.Date;
    }
}