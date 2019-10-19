using ReactiveUI;
using SonOfPicasso.UI.ViewModels;

namespace SonOfPicasso.UI.Views
{
    /// <summary>
    ///     Interaction logic for ManageFolderView.xaml
    /// </summary>
    public partial class ManageFolderView : ReactiveUserControl<ManageFolderRulesViewModel>
    {
        public ManageFolderView()
        {
            InitializeComponent();

            this.WhenActivated(disposable =>
            {
                this.OneWayBind(ViewModel, model => model.FullName, view => view.NameLabel.Content);
                this.OneWayBind(ViewModel, model => model.ManageFolderState, view => view.StatusLabel.Content);
            });
        }
    }
}