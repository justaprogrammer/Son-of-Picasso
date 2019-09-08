using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Serilog;
using SonOfPicasso.Core.Interfaces;
using SonOfPicasso.Core.Scheduling;
using SonOfPicasso.Data.Model;
using SonOfPicasso.Data.Repository;
using Directory = SonOfPicasso.Data.Model.Directory;

namespace SonOfPicasso.Core.Services
{
    public class ImageManagementService : IImageManagementService
    {
        private readonly IFileSystem _fileSystem;
        private readonly IImageLocationService _imageLocationService;
        private readonly ILogger _logger;
        private readonly ISchedulerProvider _schedulerProvider;
        private readonly Func<IUnitOfWork> _unitOfWorkFactory;

        public ImageManagementService(ILogger logger,
            IFileSystem fileSystem,
            IImageLocationService imageLocationService,
            Func<IUnitOfWork> unitOfWorkFactory,
            ISchedulerProvider schedulerProvider)
        {
            _logger = logger;
            _fileSystem = fileSystem;
            _imageLocationService = imageLocationService;
            _unitOfWorkFactory = unitOfWorkFactory;
            _schedulerProvider = schedulerProvider;
        }

        public IObservable<Image[]> ScanFolder(string path)
        {
            return Observable.StartAsync<Image[]>(async task =>
            {
                using var unitOfWork = _unitOfWorkFactory();
                
                if (!_fileSystem.Directory.Exists(path))
                    throw new SonOfPicassoException($"Path: `{path}` does not exist");

                var images = await _imageLocationService.GetImages(path)
                    .SelectMany(locatedImages => locatedImages)
                    .Where(s => !unitOfWork.ImageRepository.Get(image => image.Path == path).Any())
                    .GroupBy(s => _fileSystem.FileInfo.FromFileName(s).DirectoryName)
                    .SelectMany(groupedObservable =>
                    {
                        var directory = unitOfWork.DirectoryRepository.Get(directory => directory.Path == groupedObservable.Key)
                            .FirstOrDefault();

                        if (directory == null)
                        {
                            directory = new Directory { Path = groupedObservable.Key, Images = new List<Image>()};
                            unitOfWork.DirectoryRepository.Insert(directory);
                        }

                        return groupedObservable.Select(imagePath =>
                        {
                            var image = new Image()
                            {
                                Path = imagePath
                            };

                            unitOfWork.ImageRepository.Insert(image);
                            directory.Images.Add(image);

                            return image;
                        });
                    }).ToArray();

                unitOfWork.Save();

                return images;
            }, _schedulerProvider.TaskPool);
        }
    }
}