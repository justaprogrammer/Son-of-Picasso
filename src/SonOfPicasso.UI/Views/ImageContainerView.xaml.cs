using System;
using System.Reactive.Linq;
using ReactiveUI;
using SonOfPicasso.UI.ViewModels;

namespace SonOfPicasso.UI.Views
{
    /// <summary>
    /// Interaction logic for ImageContainerView.xaml
    /// </summary>
    public partial class ImageContainerView : ReactiveUserControl<ImageContainerViewModel>
    {
        public ImageContainerView()
        {
            InitializeComponent();

            this.WhenAny(view => view.ViewModel, change => change.Value)
                .Skip(1)
                .Subscribe(model =>
                {
                    ;
                });
        }
    }
}
