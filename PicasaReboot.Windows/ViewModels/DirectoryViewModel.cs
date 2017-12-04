﻿using System;
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

        public ReactiveList<IImageViewModel> Images { get; } = new ReactiveList<IImageViewModel>();
        public string Name { get; }

        public DirectoryViewModel(ImageService imageService, string name)
            : this(imageService, name,  new SchedulerProvider())
        {
        }

        public DirectoryViewModel(ImageService imageService, string name, ISchedulerProvider scheduler)
        {
            Name = name;
            imageService
                .ListFilesAsync(name)
                .ObserveOn(scheduler.ThreadPool)
                .SelectMany(strings => strings)
                .Select(file =>
                {
                    Log.Verbose("Creating ImageViewModel {File}", file);
                    return new ImageViewModel(imageService, file, scheduler);
                })
                .ObserveOn(scheduler.Dispatcher)
                .Subscribe(imageViewModel =>
                {
                    Log.Verbose("Populating image: {File}", imageViewModel.File);
                    Images.Add(imageViewModel);
                });

            Log.Debug("Created");
        }
    }
}