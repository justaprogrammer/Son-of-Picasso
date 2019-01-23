using System;
using System.Threading.Tasks;
using Nito.Mvvm;
using SonOfPicasso.Core.Interfaces;
using Splat;

namespace SonOfPicasso.UI.ViewModels
{
    public class ImageViewModelBitmapConveter : IImageViewModelBitmapConveter
    {
        private readonly IImageLoadingService _imageLoadingService;

        public ImageViewModelBitmapConveter(IImageLoadingService imageLoadingService)
        {
            _imageLoadingService = imageLoadingService;
        }

        public int GetAffinityForObjects(Type fromType, Type toType)
        {
            if (fromType == typeof(ImageViewModel)
                && toType == typeof(NotifyTask<IBitmap>))
            {
                return 1;
            }

            return 0;
        }

        public bool TryConvert(object @from, Type toType, object conversionHint, out object result)
        {
            var imageViewModel = (ImageViewModel)@from;

            if (toType == typeof(NotifyTask<IBitmap>))
            {
                var tcs = new TaskCompletionSource<IBitmap>();
                result = NotifyTask.Create(tcs.Task);

                _imageLoadingService
                    .LoadImageFromPath(imageViewModel.Image.Path)
                    .Subscribe(tcs.SetResult);

                return true;
            }

            result = null;

            return false;
        }
    }
}