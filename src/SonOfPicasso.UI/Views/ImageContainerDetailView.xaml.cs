using ReactiveUI;
using SonOfPicasso.UI.ViewModels;

namespace SonOfPicasso.UI.Views
{
    /// <summary>
    /// Interaction logic for ImageContainerView.xaml
    /// </summary>
    public partial class ImageContainerDetailView : ReactiveUserControl<ImageContainerViewModel>
    {
        public ImageContainerDetailView()
        {
            InitializeComponent();

            this.WhenActivated(d =>
            {
                FolderName.Content = ViewModel.Name;
            });
        }
    }
}
