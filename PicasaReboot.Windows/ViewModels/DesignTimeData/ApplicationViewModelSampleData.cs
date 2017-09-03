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

                applicationViewModelSampleData.Images.Add(new ImageView("image1.jpeg", SampleImages.Resources.image1.GetBitmapImage()));
                applicationViewModelSampleData.Images.Add(new ImageView("image2.jpeg", SampleImages.Resources.image2.GetBitmapImage()));
                applicationViewModelSampleData.Images.Add(new ImageView("image3.jpeg", SampleImages.Resources.image3.GetBitmapImage()));
                applicationViewModelSampleData.Images.Add(new ImageView("image4.jpeg", SampleImages.Resources.image4.GetBitmapImage()));

                return applicationViewModelSampleData;
            }
        }

        public IList<ImageView> Images { get; set; } = new List<ImageView>();
    }
}