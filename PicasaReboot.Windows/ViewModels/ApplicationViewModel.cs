using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using PicasaReboot.Core;
using ReactiveUI;
using Serilog;

namespace PicasaReboot.Windows.ViewModels
{
    public class ApplicationViewModel : ReactiveObject, IApplicationViewModel
    {
        private static ILogger Log { get; } = LogManager.ForContext<ApplicationViewModel>();

        string _directory;

        public string Directory
        {
            get { return _directory; }
            set { this.RaiseAndSetIfChanged(ref _directory, value); }
        }

        public ReactiveList<ImageViewModel> Images { get; } = new ReactiveList<ImageViewModel>();

        public ApplicationViewModel(ImageService imageService)
        {
            ImageService = imageService;

            this.WhenAnyValue(model => model.Directory)
                .Subscribe(directory =>
                {
                    Images.Clear();
                    if (directory != null)
                    {
                        imageService
                            .ListFilesAsync(directory)
                            .Select(images => images.Select(image => new ImageViewModel(imageService, image)))
                            .Subscribe(Images.AddRange);
                    }
                });
        }

        public ImageService ImageService { get; }
    }
}
