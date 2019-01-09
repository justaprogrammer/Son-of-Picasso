using SonOfPicasso.Core;

namespace SonOfPicasso.Windows.ViewModels.DesignTimeData
{
    public class DirectoryViewModelSampleData : IDirectoryViewModel
    {
        public static IDirectoryViewModel SampleData
        {
            get
            {
                var applicationViewModelSampleData = new DirectoryViewModelSampleData()
                {
                    Name = @"c:\Images",
                    Images = new ReactiveList<IImageViewModel>
                    {
                        new ImageViewModelSampleData
                        {
                            File = "image.jpg",
                            Image = Observable.Return(SampleImages.Resources.image1.GetBitmapImage())
                        }
                    }
                };

                return applicationViewModelSampleData;
            }
        }

        public string Name { get; set; }
        public ReactiveList<IImageViewModel> Images { get; set; }
        public ImageService ImageService { get; set; }
    }
}