using System;
using SonOfPicasso.Core.Model;
using SonOfPicasso.UI.WPF.ViewModels.Abstract;

namespace SonOfPicasso.UI.WPF.ViewModels
{
    public class ImageViewModel : ViewModelBase
    {
        public ImageViewModel(ImageRef imageRef, ImageContainerViewModel imageContainerViewModel)
        {
            ImageRef =
                imageRef ??
                throw new ArgumentNullException(nameof(imageRef));

            ImageContainerViewModel =
                imageContainerViewModel ??
                throw new ArgumentNullException(nameof(imageContainerViewModel));
        }

        public ImageRef ImageRef { get; }
        public ImageContainerViewModel ImageContainerViewModel { get; }
        public string ImageRefId => ImageRef.Key;
        public DateTime ExifDate => ImageRef.ExifDate;
        public int ImageId => ImageRef.Id;
        public string Path => ImageRef.ImagePath;
        public string ContainerKey => ImageContainerViewModel.ContainerKey;
        public ImageContainerTypeEnum ContainerType => ImageContainerViewModel.ContainerType;
        public int ContainerYear => ImageContainerViewModel.Year;
        public DateTime ContainerDate => ImageContainerViewModel.Date;
    }
}