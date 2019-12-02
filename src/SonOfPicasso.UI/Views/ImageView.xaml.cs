using System;
using System.Reactive.Linq;
using ReactiveUI;
using SonOfPicasso.UI.ViewModels;
using SonOfPicasso.UI.Windows;

namespace SonOfPicasso.UI.Views
{
    /// <summary>
    ///     Interaction logic for ImageView.xaml
    /// </summary>
    public partial class ImageView : ReactiveUserControl<ImageViewModel>
    {
        public ImageView()
        {
            InitializeComponent();

            this.WhenAny(view => view.ViewModel, change => change.Value)
                .Skip(1)
                .SelectMany(model => MainWindow.GetBitmapSourceFromPath(model.Path))
                .ObserveOnDispatcher()
                .Subscribe(source => ImageControl.Source = source);
        }

        public ImageContainerView ImageContainerView => (ImageContainerView) Parent;
        public ImageContainerListView ImageContainerListView => (ImageContainerListView) ImageContainerView.Parent;
        public MainWindow MainWindow => (MainWindow) ImageContainerListView.Parent;
    }
}