using Serilog;
using SonOfPicasso.Core.Interfaces;
using SonOfPicasso.Data;

namespace SonOfPicasso.Core.Services
{
    public class ImageManagementService : IImageManagementService
    {
        private readonly IDataContext _dataContext;
        private readonly IImageLocationService _imageLocationService;
        private readonly ILogger _logger;

        public ImageManagementService(IDataContext dataContext,
            IImageLocationService imageLocationService,
            ILogger logger)
        {
            _dataContext = dataContext;
            _imageLocationService = imageLocationService;
            _logger = logger;
        }

    }
}