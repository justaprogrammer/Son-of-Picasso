using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using Serilog;
using SonOfPicasso.Core.Interfaces;
using SonOfPicasso.Core.Models;

namespace SonOfPicasso.Core.Services
{
    public class ImageManagementService : IImageManagementService
    {
        private readonly IDataCache _dataCache;
        private readonly IImageLocationService _imageLocationService;
        private readonly ILogger _logger;

        public ImageManagementService(IDataCache dataCache,
            IImageLocationService imageLocationService,
            ILogger logger)
        {
            _dataCache = dataCache ?? throw new ArgumentNullException(nameof(dataCache));
            _imageLocationService = imageLocationService ?? throw new ArgumentNullException(nameof(imageLocationService));
            _logger = logger;
        }

    }
}