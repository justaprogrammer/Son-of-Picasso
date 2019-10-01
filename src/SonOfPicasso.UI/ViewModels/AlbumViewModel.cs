using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ReactiveUI;
using SonOfPicasso.Data.Model;
using SonOfPicasso.UI.Injection;
using SonOfPicasso.UI.ViewModels.Abstract;
using SonOfPicasso.UI.ViewModels.Interfaces;
using SonOfPicasso.UI.Views;

namespace SonOfPicasso.UI.ViewModels
{
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