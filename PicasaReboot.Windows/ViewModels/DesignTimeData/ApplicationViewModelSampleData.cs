using System.Collections.ObjectModel;

namespace PicasaReboot.Windows.ViewModels.DesignTimeData
{
    public class ApplicationViewModelSampleData : IApplicationViewModel
    {
        public static IApplicationViewModel SampleData
        {
            get
            {
                var applicationViewModelSampleData = new ApplicationViewModelSampleData();

                return applicationViewModelSampleData;
            }
        }

        public ObservableCollection<ImageViewModel> Images { get; set; } = new ObservableCollection<ImageViewModel>();
    }
}