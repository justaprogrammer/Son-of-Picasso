using System;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using PicasaReboot.Core;
using PicasaReboot.Core.Logging;
using PicasaReboot.Core.Scheduling;
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

        public ReactiveList<IImageViewModel> Images { get; } = new ReactiveList<IImageViewModel>();

        public ApplicationViewModel(ImageService imageService)
            : this(imageService,  new SchedulerProvider())
        {
        }

        public ApplicationViewModel(ImageService imageService, ISchedulerProvider scheduler)
        {
            ImageService = imageService;

            this.WhenAnyValue(model => model.Directory)
                .Subscribe(directory =>
                {
                    Log.Verbose("Directory Changed: {directory}", directory);

                    Images.Clear();
                    if (directory != null)
                    {
                        imageService
                            .ListFilesAsync(directory)
                            .ObserveOn(scheduler.ThreadPool)
                            .SelectMany(strings => strings)
                            .Select(s =>
                            {
                                Log.Verbose("Creating ImageViewModel {File}", s);
                                return new ImageViewModel(imageService, s);
                            })
                            .ObserveOn(scheduler.Dispatcher)
                            .Subscribe(imageViewModel =>
                            {
                                Log.Verbose("Populating image: {File}", imageViewModel.File);
                                Images.Add(imageViewModel);
                            });
                    }
                });

            Log.Debug("Created");
        }

        public ImageService ImageService { get; }
    }
}