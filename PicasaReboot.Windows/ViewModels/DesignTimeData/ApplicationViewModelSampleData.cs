using System.Collections.Generic;
using PicasaReboot.Core.Extensions;

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

        public IList<ImageViewModel> Images { get; set; } = new List<ImageViewModel>();
    }
}