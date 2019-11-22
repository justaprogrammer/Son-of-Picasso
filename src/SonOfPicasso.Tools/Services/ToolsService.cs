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

        public ToolsService(ILogger logger, IFileSystem fileSystem)
        {
            _logger = logger;
            _fileSystem = fileSystem;
        }
    }
}