using System;
using System.Reactive.Linq;
using PicasaReboot.Core;
using PicasaReboot.Core.Logging;
using PicasaReboot.Core.Scheduling;
using ReactiveUI;
using Serilog;

namespace PicasaReboot.Windows.ViewModels
{
    public class DirectoryViewModel : ReactiveObject, IDirectoryViewModel
    {
        private static ILogger Log { get; } = LogManager.ForContext<DirectoryViewModel>();

        private string _name;

        public string Name
        {
            get { return _name; }
            set { this.RaiseAndSetIfChanged(ref _name, value); }
        }

        public ReactiveList<IImageViewModel> Images { get; } = new ReactiveList<IImageViewModel>();

        public DirectoryViewModel(ImageService imageService)
            : this(imageService,  new SchedulerProvider())
        {
        }

        public DirectoryViewModel(ImageService imageService, ISchedulerProvider scheduler)
        {
            this.WhenAnyValue(model => model.Name)
                .Subscribe(directory =>
                {
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
    }
}