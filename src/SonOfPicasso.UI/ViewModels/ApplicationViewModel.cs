using System;
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
using SonOfPicasso.UI.Interfaces;

namespace SonOfPicasso.UI.ViewModels
{
    public class ApplicationViewModel : ReactiveObject, IApplicationViewModel
    {
        private readonly Func<IImageFolderViewModel> _imageFolderViewModelFactory;
        private readonly IImageManagementService _imageManagementService;
        private readonly Func<IImageViewModel> _imageViewModelFactory;
        private readonly ILogger _logger;
        private readonly ISchedulerProvider _schedulerProvider;

        public ApplicationViewModel(ILogger logger,
            ISchedulerProvider schedulerProvider,
            IImageManagementService imageManagementService,
            Func<IImageViewModel> imageViewModelFactory,
            Func<IImageFolderViewModel> imageFolderViewModelFactory, 
            ViewModelActivator activator)
        {
            _logger = logger;
            _schedulerProvider = schedulerProvider;
            _imageManagementService = imageManagementService;
            _imageViewModelFactory = imageViewModelFactory;
            _imageFolderViewModelFactory = imageFolderViewModelFactory;
            Activator = activator;

            var images = new ObservableCollectionExtended<IImageViewModel>();
            Images = images;

            var imageFolders = new ObservableCollectionExtended<IImageFolderViewModel>();
            ImageFolders = imageFolders;

            AddFolder = ReactiveCommand.CreateFromObservable<string, Unit>(ExecuteAddFolder);
            NewAlbum = ReactiveCommand.CreateFromObservable(ExecuteNewAlbum);

            ImageCache = new SourceCache<IImageViewModel, string>(model => model.Path);
            ImageFolderCache = new SourceCache<IImageFolderViewModel, string>(model => model.Path);

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

        private IImageFolderViewModel CreateImageFolderViewModel(Folder folder)
        {
            var imageFolderViewModel = _imageFolderViewModelFactory();
            imageFolderViewModel.Initialize(folder);
            return imageFolderViewModel;
        }

        private SourceCache<IImageFolderViewModel, string> ImageFolderCache { get; set; }

        private SourceCache<IImageViewModel, string> ImageCache { get; set; }

        public ObservableCollection<IImageViewModel> Images { get; }

        public ObservableCollection<IImageFolderViewModel> ImageFolders { get; }

        public ReactiveCommand<string, Unit> AddFolder { get; }
        public ReactiveCommand<Unit, Unit> NewAlbum { get; }

        public ViewModelActivator Activator { get; }

        private IImageViewModel CreateImageViewModel(Image image)
        {
            var imageViewModel = _imageViewModelFactory();
            imageViewModel.Initialize(image);
            return imageViewModel;
        }

        private IObservable<Unit> ExecuteNewAlbum()
        {
            return Observable.Return(Unit.Default);
        }

        private IObservable<Unit> ExecuteAddFolder(string addPath)
        {
            var scanFolder = _imageManagementService.ScanFolder(addPath)
                .AsObservable()
                .SelectMany(images => images);

            var addImages = scanFolder
                .ToArray()
                .Select(images => {
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
        }
    }
}