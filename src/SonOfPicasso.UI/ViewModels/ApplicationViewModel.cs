﻿using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using Serilog;
using SonOfPicasso.Core.Interfaces;
using SonOfPicasso.Core.Scheduling;
using SonOfPicasso.Data.Model;
using SonOfPicasso.UI.ViewModels.Interfaces;

namespace SonOfPicasso.UI.ViewModels
{
    public class ApplicationViewModel : ReactiveObject, IActivatableViewModel
    {
        private readonly Func<ImageFolderViewModel> _imageFolderViewModelFactory;
        private readonly IImageManagementService _imageManagementService;
        private readonly Func<ImageViewModel> _imageViewModelFactory;
        private readonly ILogger _logger;
        private readonly ISchedulerProvider _schedulerProvider;

        public ApplicationViewModel(ILogger logger,
            ISchedulerProvider schedulerProvider,
            IImageManagementService imageManagementService,
            Func<ImageViewModel> imageViewModelFactory,
            Func<ImageFolderViewModel> imageFolderViewModelFactory,
            ViewModelActivator activator)
        {
            _logger = logger;
            _schedulerProvider = schedulerProvider;
            _imageManagementService = imageManagementService;
            _imageViewModelFactory = imageViewModelFactory;
            _imageFolderViewModelFactory = imageFolderViewModelFactory;
            Activator = activator;

            var images = new ObservableCollectionExtended<ImageViewModel>();
            Images = images;

            var imageFolders = new ObservableCollectionExtended<ImageFolderViewModel>();
            ImageFolders = imageFolders;

            AddFolder = ReactiveCommand.CreateFromObservable<Unit, Unit>(ExecuteAddFolder,
                outputScheduler: schedulerProvider.TaskPool);
            AddFolderInteraction = new Interaction<Unit, string>();

            NewAlbum = ReactiveCommand.CreateFromObservable(ExecuteNewAlbum);
            NewAlbumInteraction = new Interaction<Unit, AddAlbumViewModel>();

            ImageCache = new SourceCache<ImageViewModel, string>(model => model.Path);
            ImageFolderCache = new SourceCache<IImageContainerViewModel, string>(model => model.Path);

            this.WhenActivated(d =>
            {
                d(ImageCache
                    .Connect()
                    .ObserveOn(_schedulerProvider.MainThreadScheduler)
                    .Bind(images)
                    .Subscribe());

                d(ImageFolderCache
                    .Connect()
                    .ObserveOn(_schedulerProvider.MainThreadScheduler)
                    .Bind(imageFolders)
                    .Subscribe());

                var allImages = _imageManagementService.GetImagesWithDirectoryAndExif()
                    .Publish();

                d(ImageCache
                    .PopulateFrom(allImages
                        .SelectMany(i => i)
                        .Select(CreateImageViewModel)));

                d(ImageFolderCache
                    .PopulateFrom(allImages
                        .SelectMany(i => i)
                        .Select(image => image.Folder)
                        .Distinct(directory => directory.Path)
                        .Select(CreateImageFolderViewModel)));

                d(allImages.Connect());
            });
        }

        public Interaction<Unit, string> AddFolderInteraction { get; set; }

        public Interaction<Unit, AddAlbumViewModel> NewAlbumInteraction { get; set; }

        private SourceCache<ImageFolderViewModel, string> ImageFolderCache { get; }

        private SourceCache<ImageViewModel, string> ImageCache { get; }

        public ObservableCollection<ImageViewModel> Images { get; }

        public ObservableCollection<ImageFolderViewModel> ImageFolders { get; }

        public ReactiveCommand<Unit, Unit> AddFolder { get; }

        public ReactiveCommand<Unit, Unit> NewAlbum { get; }

        public ViewModelActivator Activator { get; }

        private ImageFolderViewModel CreateImageFolderViewModel(Folder folder)
        {
            var imageFolderViewModel = _imageFolderViewModelFactory();
            imageFolderViewModel.Initialize(folder);
            return imageFolderViewModel;
        }

        private ImageViewModel CreateImageViewModel(Image image)
        {
            var imageViewModel = _imageViewModelFactory();
            imageViewModel.Initialize(image);
            return imageViewModel;
        }

        private IObservable<Unit> ExecuteNewAlbum()
        {
            return NewAlbumInteraction.Handle(Unit.Default)
                .ObserveOn(_schedulerProvider.TaskPool)
                .Select(album =>
                {
                    if (album == null)
                        return Observable.Return(Unit.Default);

                    return _imageManagementService.CreateAlbum(album.AlbumName)
                        .Select(album1 => Unit.Default);
                })
                .SelectMany(observable => observable);
        }

        private IObservable<Unit> ExecuteAddFolder(Unit unit)
        {
            return AddFolderInteraction.Handle(Unit.Default)
                .ObserveOn(_schedulerProvider.TaskPool)
                .Select(s =>
                {
                    if (s == null) return Observable.Return(Unit.Default);

                    var scanFolder = _imageManagementService.ScanFolder(s)
                        .AsObservable()
                        .SelectMany(images => images);

                    var addImages = scanFolder
                        .ToArray()
                        .Select(images =>
                        {
                            ImageCache.AddOrUpdate(images.Select(CreateImageViewModel));
                            return Unit.Default;
                        });

                    var addDirectories = scanFolder
                        .Select(image => image.Folder)
                        .GroupBy(directory => directory.Path)
                        .Select(groupedObservable => groupedObservable.FirstAsync())
                        .SelectMany(observable1 => observable1)
                        .Select(directory => (directory, ImageFolderCache.Lookup(directory.Path)));

                    var addFolders = addDirectories
                        .Where(tuple => !tuple.Item2.HasValue)
                        .Select(tuple => tuple.directory)
                        .ToArray()
                        .Select(directories =>
                        {
                            ImageFolderCache.AddOrUpdate(directories.Select(CreateImageFolderViewModel));
                            return Unit.Default;
                        });

                    var updateFolders = addDirectories
                        .Where(tuple => tuple.Item2.HasValue)
                        .ToArray()
                        .Select(tuples =>
                        {
                            foreach (var tuple in tuples)
                                tuple.Item2.Value.Initialize(tuple.directory);

                            return Unit.Default;
                        });

                    return addImages.Zip(addFolders, updateFolders, (unit, _, __) => unit);
                })
                .SelectMany(observable => observable);
        }
    }
}