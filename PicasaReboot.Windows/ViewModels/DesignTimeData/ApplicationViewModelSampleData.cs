using System.Collections.Generic;
using System.Windows.Media.Imaging;
using PicasaReboot.Windows.Properties;

namespace PicasaReboot.Windows.ViewModels.DesignTimeData
{
    public class ApplicationViewModelSampleData : IApplicationViewModel
    {
        public static IApplicationViewModel SampleData
        {
            get
            {

                var applicationViewModelSampleData = new ApplicationViewModelSampleData();
                var imageView = new ImageView("image1.jpeg", null);

                applicationViewModelSampleData.Images.Add(imageView);
                return applicationViewModelSampleData;
            }
        }

        public IList<ImageView> Images { get; set; } = new List<ImageView>();
    }
}