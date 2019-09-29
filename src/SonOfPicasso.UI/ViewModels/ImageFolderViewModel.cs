using System;
using ReactiveUI;
using SonOfPicasso.Data.Model;
using SonOfPicasso.UI.Injection;
using SonOfPicasso.UI.ViewModels.Abstract;
using SonOfPicasso.UI.ViewModels.Interfaces;
using SonOfPicasso.UI.Views;

namespace SonOfPicasso.UI.ViewModels
{
    [ViewModelView(typeof(ImageFolderViewControl))]
    public class ImageFolderViewModel : ViewModelBase, IImageContainerViewModel
    {
        private Folder _imageFolderModel;

        public ImageFolderViewModel(ViewModelActivator activator) : base(activator)
        {
        }

        public string Path => _imageFolderModel.Path;

        public void Initialize(Folder imageFolderModel)
        {
            _imageFolderModel = imageFolderModel ?? throw new ArgumentNullException(nameof(imageFolderModel));
        }

        public string ContainerId => $"Folder{_imageFolderModel.Id}";
    }
}