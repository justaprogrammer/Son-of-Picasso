﻿using System;
using ReactiveUI;
using SonOfPicasso.Core.Models;
using SonOfPicasso.Data.Model;
using SonOfPicasso.UI.Injection;
using SonOfPicasso.UI.Interfaces;
using SonOfPicasso.UI.Views;

namespace SonOfPicasso.UI.ViewModels
{
    [ViewModelView(typeof(ImageFolderViewControl))]
    public class ImageFolderViewModel : ReactiveObject, IImageFolderViewModel
    {
        private Directory _imageFolderModel;

        public string Path => _imageFolderModel.Path;

        public void Initialize(Directory imageFolderModel)
        {
            _imageFolderModel = imageFolderModel ?? throw new ArgumentNullException(nameof(imageFolderModel));
        }
    }
}