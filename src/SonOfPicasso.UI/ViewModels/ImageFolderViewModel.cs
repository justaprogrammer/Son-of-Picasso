using System;
using ReactiveUI;
using SonOfPicasso.Core.Models;
using SonOfPicasso.UI.Injection;
using SonOfPicasso.UI.Interfaces;
using SonOfPicasso.UI.Views;

namespace SonOfPicasso.UI.ViewModels
{
    [ViewModelView(typeof(ImageFolderViewControl))]
    public class ImageFolderViewModel : ReactiveObject, IImageFolderViewModel
    {
        private ImageFolderModel _imageFolderModel;

        public string Path => _imageFolderModel.Path;

        public void Initialize(ImageFolderModel imageFolderModel)
        {
            _imageFolderModel = imageFolderModel ?? throw new ArgumentNullException(nameof(imageFolderModel));
        }
    }
}