using System;
using System.Collections.Generic;
using ReactiveUI;
using SonOfPicasso.Core.Model;
using SonOfPicasso.UI.ViewModels.Abstract;

namespace SonOfPicasso.UI.ViewModels
{
    public class ImageContainerViewModel : ViewModelBase
    {
        private ImageContainer _imageContainer;

        public ImageContainerViewModel(ViewModelActivator activator) : base(activator)
        {
        }

        public string Name => _imageContainer.Name;

        public string ContainerId => _imageContainer.Id;

        public ImageContainerTypeEnum ContainerType => _imageContainer.ContainerType;

        public DateTime Date => _imageContainer.Date;

        public void Initialize(ImageContainer imageContainer)
        {
            _imageContainer = imageContainer ?? throw new ArgumentNullException(nameof(imageContainer));
        }
    }
}