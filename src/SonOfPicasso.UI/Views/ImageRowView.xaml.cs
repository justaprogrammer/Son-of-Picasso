using ReactiveUI;
using SonOfPicasso.UI.ViewModels;

namespace SonOfPicasso.UI.Views
{
    /// <summary>
    /// Interaction logic for ImageRowView.xaml
    /// </summary>
    public partial class ImageRowView : ReactiveUserControl<ImageRefRowViewModel>, IActivatableView
    {
        public ImageRowView()
        {
            InitializeComponent();

            this.WhenActivated(d =>
            {
                d(this.OneWayBind(ViewModel,
                    model => model.ImageRefViewModels,
                    view => view.RowItems.ItemsSource));
            });
        }
    }
}
