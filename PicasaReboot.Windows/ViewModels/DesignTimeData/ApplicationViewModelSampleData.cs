using PicasaReboot.Core;
using PicasaReboot.Core.Extensions;
using ReactiveUI;

namespace PicasaReboot.Windows.ViewModels.DesignTimeData
{
    public class ApplicationViewModelSampleData : IApplicationViewModel
    {
        public static IApplicationViewModel SampleData
        {
            get
            {
                var applicationViewModelSampleData = new ApplicationViewModelSampleData()
                {
                    Directory = @"c:\Images",
                    Images = new ReactiveList<IImageViewModel>
                    {
                        new ImageViewModelSampleData
                        {
                            File = "image.jpg",
                            Image = SampleImages.Resources.image1.GetBitmapImage()
                        }
                    }
                };

                return applicationViewModelSampleData;
            }
        }

        public string Directory { get; set; }
        public ReactiveList<IImageViewModel> Images { get; set; }
        public ImageService ImageService { get; set; }
    }
}