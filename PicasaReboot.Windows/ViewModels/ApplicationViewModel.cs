using System.Collections.Generic;
using System.Collections.ObjectModel;
using PicasaReboot.Core;
using ReactiveUI;

namespace PicasaReboot.Windows.ViewModels
{
    public class ApplicationViewModel : ReactiveObject, IApplicationViewModel
    {
        public ApplicationViewModel(ImageService imageService)
        {
            ImageService = imageService;
        }

        public void Initialize(string directory)
        {
            var files = ImageService.ListFiles(directory);
            var imageViewModels = new ObservableCollection<ImageViewModel>();
        }

        private IList<ImageViewModel> _images;
        public ImageService ImageService { get; }

        public IList<ImageViewModel> Images
        {
            get { return _images; }
            set { this.RaiseAndSetIfChanged(ref _images, value); }
        }
    }
}
