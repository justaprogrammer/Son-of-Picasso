using System;
using System.Collections.Generic;
using ReactiveUI;
using SonOfPicasso.Core.Model;
using SonOfPicasso.Core.Scheduling;
using SonOfPicasso.UI.ViewModels.Abstract;

namespace SonOfPicasso.UI.ViewModels
{
    public class ImageContainerViewModel : ViewModelBase
    {
        private readonly ISchedulerProvider _schedulerProvider;

        private ImageContainer _imageContainer;

        public ImageContainerViewModel(
            ViewModelActivator activator, 
            ISchedulerProvider schedulerProvider) : base(activator)
        {
            _schedulerProvider = schedulerProvider;
        }

        public string Name => _imageContainer.Name;

        public string ContainerId => _imageContainer.Id;

        public ImageContainerTypeEnum ContainerType => _imageContainer.ContainerType;

        public IList<ImageRef> ImageRefs => _imageContainer.ImageRefs;

        public DateTime Date => _imageContainer.Date;

        public ApplicationViewModel ApplicationViewModel { get; private set; }

        public void Initialize(ImageContainer imageContainer, ApplicationViewModel applicationViewModel)
        {
            _imageContainer = imageContainer ?? throw new ArgumentNullException(nameof(imageContainer));
            ApplicationViewModel =
                applicationViewModel ?? throw new ArgumentNullException(nameof(applicationViewModel));
        }
    }
}