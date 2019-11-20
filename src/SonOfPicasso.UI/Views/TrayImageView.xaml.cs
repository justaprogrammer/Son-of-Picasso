using System;
using System.Reactive.Disposables;
using ReactiveUI;
using SonOfPicasso.UI.ViewModels;
using Splat;

namespace SonOfPicasso.UI.Views
{
    /// <summary>
    /// Interaction logic for TrayImageView.xaml
    /// </summary>
    public partial class TrayImageView : ReactiveUserControl<TrayImageViewModel>
    {
        public TrayImageView()
        {
            InitializeComponent();

            this.WhenActivated(d =>
            {
                this.OneWayBind(ViewModel,
                    model => model.Pinned,
                    view => view.ImageOverlay.Visibility);
                
                this.OneWayBind(ViewModel,
                    model => model.Image,
                    view => view.ImageBitmap.Source);
            });
        }
    }
}
