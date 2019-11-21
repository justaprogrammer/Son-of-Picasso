﻿using System;
using System.Reactive.Linq;
using System.Windows.Media.Imaging;
using ReactiveUI;
using SonOfPicasso.Core.Interfaces;
using SonOfPicasso.Core.Model;
using SonOfPicasso.Core.Scheduling;
using SonOfPicasso.UI.ViewModels.Abstract;
using Splat;

namespace SonOfPicasso.UI.ViewModels
{
    public class ImageViewModel : ViewModelBase
    {
        private readonly ObservableAsPropertyHelper<BitmapSource> _image;

        public ImageViewModel(IImageLoadingService imageLoadingService, ISchedulerProvider schedulerProvider,
            ImageRef imageRef, ImageContainerViewModel imageContainerViewModel)
        {
            if (imageLoadingService == null) throw new ArgumentNullException(nameof(imageLoadingService));
            if (schedulerProvider == null) throw new ArgumentNullException(nameof(schedulerProvider));

            ImageRef = 
                imageRef ?? 
                throw new ArgumentNullException(nameof(imageRef));

            ImageContainerViewModel =
                imageContainerViewModel ??
                throw new ArgumentNullException(nameof(imageContainerViewModel));

            imageLoadingService
                .LoadImageFromPath(Path)
                .Select(bitmap => bitmap.ToNative())
                .ObserveOn(schedulerProvider.MainThreadScheduler)
                .ToProperty(this, nameof(Image), out _image, deferSubscription: true);
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
        public BitmapSource Image => _image.Value;
    }
}