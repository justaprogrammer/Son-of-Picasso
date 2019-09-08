using System;
using System.Diagnostics;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using Serilog;
using SonOfPicasso.Core.Interfaces;
using SonOfPicasso.Data;
using SonOfPicasso.Data.Model;
using SonOfPicasso.Data.Repository;
using Directory = SonOfPicasso.Data.Model.Directory;

namespace SonOfPicasso.Core.Services
{
    public class ImageManagementService : IImageManagementService
    {
        private readonly Func<IUnitOfWork> _unitOfWorkFactory;
        private readonly IFileSystem _fileSystem;
        private readonly IImageLocationService _imageLocationService;
        private readonly ILogger _logger;

        public ImageManagementService(ILogger logger,
            IFileSystem fileSystem,
            IImageLocationService imageLocationService,
            Func<IUnitOfWork> unitOfWorkFactory)
        {
            _logger = logger;
            _fileSystem = fileSystem;
            _imageLocationService = imageLocationService;
            _unitOfWorkFactory = unitOfWorkFactory;
        }

        public void AddFolder(string path)
        {
            using var unitOfWork = _unitOfWorkFactory();
            var paths = unitOfWork.DirectoryRepository.Get()
                .Select(directory => directory.Path)
                .ToArray();

            if (paths.Any(s => path == s || path.IsSubDirectoryOf(s)))
            {
                return;
            }

            unitOfWork.DirectoryRepository.Insert(new Directory { Path = path });
            unitOfWork.Save();
        }
    }

    public static class StringExtensions
    {
        // https://stackoverflow.com/a/23354773/104877
        public static bool IsSubDirectoryOf(this string candidate, string other)
        {
            var isChild = false;
            try
            {
                var candidateInfo = new DirectoryInfo(candidate);
                var otherInfo = new DirectoryInfo(other);

                while (candidateInfo.Parent != null)
                {
                    if (candidateInfo.Parent.FullName == otherInfo.FullName)
                    {
                        isChild = true;
                        break;
                    }
                    else candidateInfo = candidateInfo.Parent;
                }
            }
            catch (Exception error)
            {
                var message = String.Format("Unable to check directories {0} and {1}: {2}", candidate, other, error);
                Trace.WriteLine(message);
            }

            return isChild;
        }
    }
}