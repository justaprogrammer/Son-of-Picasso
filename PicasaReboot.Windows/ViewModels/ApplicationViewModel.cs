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
            _images = new ObservableCollection<ImageViewModel>();
        }

        public void Initialize(string directory)
        {
            var files = ImageService.ListFiles(directory);
            foreach (var file in files)
            {
                _images.Add(new ImageViewModel(ImageService, file));
            }
        }

        private ObservableCollection<ImageViewModel> _images;
        public ImageService ImageService { get; }

        public ObservableCollection<ImageViewModel> Images
        {
            get { return _images; }
            set { this.RaiseAndSetIfChanged(ref _images, value); }
        }
    }
}
