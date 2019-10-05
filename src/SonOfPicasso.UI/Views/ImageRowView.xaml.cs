using System.Reactive.Disposables;
using ReactiveUI;
using SonOfPicasso.UI.ViewModels;

namespace SonOfPicasso.UI.Views
{
    /// <summary>
    ///     Interaction logic for ImageRowView.xaml
    /// </summary>
    public partial class ImageRowView : ReactiveUserControl<ImageRowViewModel>, IActivatableView
    {
        public ImageRowView()
        {
            InitializeComponent();

            this.WhenActivated(d =>
            { 
                this.OneWayBind(ViewModel,
                    model => model.ImageViewModels,
                    view => view.RowItems.ItemsSource)
                    .DisposeWith(d);

                this.Bind(ViewModel,
                        model => model.SelectedImage,
                        view => view.RowItems.SelectedItem)
                    .DisposeWith(d);
            });
        }
    }
}