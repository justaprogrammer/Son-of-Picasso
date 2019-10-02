using System;
using System.Collections.Generic;
using System.Linq;
using ReactiveUI;
using SonOfPicasso.Data.Model;
using SonOfPicasso.UI.ViewModels.Abstract;
using SonOfPicasso.UI.ViewModels.Interfaces;

namespace SonOfPicasso.UI.ViewModels
{
    public class ImageFolderViewModel : ViewModelBase, IImageContainerViewModel
    {
        private Folder _imageFolderModel;

        public ImageFolderViewModel(ViewModelActivator activator) : base(activator)
        {
        }

        public string Path => _imageFolderModel.Path;

        public string ContainerId => GetContainerId(_imageFolderModel);
        
        public ContainerTypeEnum ContainerType => ContainerTypeEnum.Folder;
        
        public DateTime Date => _imageFolderModel.Date;

        public IList<int> ImageIds => _imageFolderModel.Images.Select(image => image.Id).ToArray();

        public void Initialize(Folder imageFolderModel)
        {
            _imageFolderModel = imageFolderModel ?? throw new ArgumentNullException(nameof(imageFolderModel));
        }

        public static string GetContainerId(Folder folder)
        {
            return $"Folder{folder.Id}";
        }
    }
}