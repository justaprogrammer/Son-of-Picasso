﻿using System;
using System.Collections.Generic;
using System.Linq;
using ReactiveUI;
using SonOfPicasso.Data.Model;
using SonOfPicasso.UI.ViewModels.Abstract;
using SonOfPicasso.UI.ViewModels.Interfaces;

namespace SonOfPicasso.UI.ViewModels
{
    public class AlbumViewModel : ViewModelBase, IImageContainerViewModel
    {
        private Album _albumModel;

        public AlbumViewModel(ViewModelActivator activator) : base(activator)
        {
        }

        public string Name => _albumModel.Name;

        public string ContainerId => CreateContainerId(_albumModel);

        public ContainerTypeEnum ContainerType => ContainerTypeEnum.Album;

        public DateTime Date => _albumModel.Date;

        public IList<int> ImageIds => _albumModel.AlbumImages.Select(image => image.Id).ToArray();

        public void Initialize(Album albumModel)
        {
            _albumModel = albumModel ?? throw new ArgumentNullException(nameof(albumModel));
        }

        public static string CreateContainerId(Album albumModel)
        {
            return $"Album{albumModel.Id}";
        }
    }
}