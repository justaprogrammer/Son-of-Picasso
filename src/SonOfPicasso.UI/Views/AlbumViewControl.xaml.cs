using System.Reactive.Disposables;
using ReactiveUI;
using SonOfPicasso.UI.ViewModels;

namespace SonOfPicasso.UI.Views
{
    /// <summary>
    /// Interaction logic for AlbumViewControl.xaml
    /// </summary>
    public partial class AlbumViewControl : ReactiveUserControl<AlbumViewModel>
    {
        public AlbumViewControl()
        {
            InitializeComponent();


            this.WhenActivated(disposable =>
            {
                this.OneWayBind(ViewModel,
                        model => model.Name,
                        window => window.AlbumName.Content)
                    .DisposeWith(disposable);
            });

        }
    }
}
