using System;
using ReactiveUI;
using SonOfPicasso.Data.Model;
using SonOfPicasso.UI.Injection;
using SonOfPicasso.UI.Views;

namespace SonOfPicasso.UI.ViewModels
{
    [ViewModelView(typeof(ImageFolderViewControl))]
    public class ImageFolderViewModel : ReactiveObject, IActivatableViewModel
    {
        private Folder _imageFolderModel;

        public ImageFolderViewModel(ViewModelActivator activator)
        {
            Activator = activator;
        }

        public string Path => _imageFolderModel.Path;

        public void Initialize(Folder imageFolderModel)
        {
            _imageFolderModel = imageFolderModel ?? throw new ArgumentNullException(nameof(imageFolderModel));
        }

        public ViewModelActivator Activator { get; }
    }
}