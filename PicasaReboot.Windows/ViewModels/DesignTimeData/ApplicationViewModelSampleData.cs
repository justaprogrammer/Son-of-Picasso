using System.Collections.Generic;

namespace PicasaReboot.Windows.ViewModels.DesignTimeData
{
    public class ApplicationViewModelSampleData : IApplicationViewModel
    {
        public static IApplicationViewModel SampleData
        {
            get
            {
                var applicationViewModelSampleData = new ApplicationViewModelSampleData();
                applicationViewModelSampleData.Images.Add("image1.jpeg");

                return applicationViewModelSampleData;
            }
        }

        public IList<string> Images { get; set; } = new List<string>();
    }
}