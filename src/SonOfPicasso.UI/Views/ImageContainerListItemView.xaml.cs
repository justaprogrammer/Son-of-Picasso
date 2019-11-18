﻿using System.Reactive.Disposables;
using ReactiveUI;
using SonOfPicasso.UI.ViewModels;

namespace SonOfPicasso.UI.Views
{
    /// <summary>
    /// Interaction logic for ImageContainerView.xaml
    /// </summary>
    public partial class ImageContainerListItemView : ReactiveUserControl<ImageContainerViewModel>
    {
        public ImageContainerListItemView()
        {
            InitializeComponent();

            this.WhenActivated(d =>
            {
                FolderName.Content = ViewModel.Name;
            });
        }
    }
}
