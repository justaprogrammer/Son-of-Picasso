using System;
using System.IO.Abstractions;
using System.Reactive;
using Bogus;
using Serilog;
using SonOfPicasso.Core.Interfaces;

namespace SonOfPicasso.Tools.Services
{
    public class ToolsService
    {
        protected internal static Faker Faker = new Faker();

        private readonly ILogger _logger;
        private readonly IFileSystem _fileSystem;
        private readonly IDataCache _dataCache;

        public ToolsService(ILogger logger, IFileSystem fileSystem, IDataCache dataCache)
        {
            _logger = logger;
            _fileSystem = fileSystem;
            _dataCache = dataCache;
        }

        public IObservable<Unit> ClearCache()
        {
            return _dataCache.Clear();
        }
    }
}