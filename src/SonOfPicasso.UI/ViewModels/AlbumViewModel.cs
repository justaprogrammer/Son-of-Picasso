using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ReactiveUI;
using SonOfPicasso.Core.Model;
using SonOfPicasso.Data.Model;
using SonOfPicasso.UI.Injection;
using SonOfPicasso.UI.ViewModels.Abstract;
using SonOfPicasso.UI.ViewModels.Interfaces;
using SonOfPicasso.UI.Views;

namespace SonOfPicasso.UI.ViewModels
{
    public class ImageContainerViewModel : ViewModelBase, IImageContainerViewModel
    {
        private ImageContainer _imageContainer;

        public ImageContainerViewModel(ViewModelActivator activator) : base(activator)
        {
        }

        public string Name => _imageContainer.Name;

        public string ContainerId => _imageContainer.Id;

        public ContainerTypeEnum ContainerType => ContainerTypeEnum.Album;

        public DateTime Date => _imageContainer.Date;

        public IList<int> ImageIds => throw new NotImplementedException();

        public void Initialize(ImageContainer imageContainer)
        {
            _imageContainer = imageContainer ?? throw new ArgumentNullException(nameof(imageContainer));
        }
    }

    [ViewModelView(typeof(AlbumViewControl))]
    public class AlbumViewModel : ViewModelBase, IImageContainerViewModel
    {
        private Album _albumModel;

        public AlbumViewModel(ViewModelActivator activator) : base(activator)
        {
        }

        public string Name => _albumModel.Name;

        public string ContainerId => GetContainerId(_albumModel);

        public ContainerTypeEnum ContainerType => ContainerTypeEnum.Album;

        public DateTime Date => _albumModel.Date;

        public IList<int> ImageIds => _albumModel.AlbumImages.Select(image => image.Id).ToArray();

        public void Initialize(Album albumModel)
        {
            _albumModel = albumModel ?? throw new ArgumentNullException(nameof(albumModel));
        }

        public static string GetContainerId(Album album)
        {
            return $"Album{album.Id}";
        }
    }
}