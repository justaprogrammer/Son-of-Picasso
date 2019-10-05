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
                d(this.OneWayBind(ViewModel,
                    model => model.ImageViewModels,
                    view => view.RowItems.ItemsSource));

                d(this.Bind(ViewModel,
                    model => model.ImageContainerViewModel.ApplicationViewModel.SelectedItem,
                    view => view.RowItems.SelectedItem,
                    vmToViewConverter: model =>
                    {
                        return (object) model;
                    });
            });
        }
    }
}