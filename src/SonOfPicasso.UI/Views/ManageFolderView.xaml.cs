using ReactiveUI;
using SonOfPicasso.UI.Interfaces;
using SonOfPicasso.UI.Services;
using SonOfPicasso.UI.ViewModels;

namespace SonOfPicasso.UI.Views
{
    /// <summary>
    ///     Interaction logic for ManageFolderView.xaml
    /// </summary>
    public partial class ManageFolderView : ReactiveUserControl<FolderRuleViewModel>
    {
        public ManageFolderView(ISvgImageProvider svgImageProvider)
        {
            InitializeComponent();


            this.WhenActivated(disposable =>
            {
                this.ImageIcon.Source = svgImageProvider.Folder;
                this.OneWayBind(ViewModel, model => model.Name, view => view.NameLabel.Content);
                this.OneWayBind(ViewModel, model => model.ManageFolderState, view => view.StatusLabel.Content);
            });
        }
    }
}