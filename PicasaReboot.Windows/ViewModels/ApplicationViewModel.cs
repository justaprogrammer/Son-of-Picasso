using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using PicasaReboot.Core;
using ReactiveUI;
using Serilog;
using Log = PicasaReboot.Core.Log;

namespace PicasaReboot.Windows.ViewModels
{
    public class ApplicationViewModel : ReactiveObject, IApplicationViewModel
    {
        private static ILogger Log { get; } = LogManager.ForContext<ApplicationViewModel>();

        private readonly string _directory;

        public ApplicationViewModel(ImageService imageService, string directory)
        {
            _directory = directory;
            ImageService = imageService;

            Log.Debug("Getting images");

            var listFiles = ImageService.ListFiles(_directory).Take(5);

            foreach (var file in listFiles)
            {
                Log.Debug("Image");
                Images.Add(new ImageViewModel(imageService, file));
            }
        }

        public ImageService ImageService { get; }

        public ObservableCollection<ImageViewModel> Images { get; } = new ObservableCollection<ImageViewModel>();
    }
}
