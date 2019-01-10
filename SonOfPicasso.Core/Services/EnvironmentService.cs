using System;
using SonOfPicasso.Core.Interfaces;

namespace SonOfPicasso.Core.Services
{
    public class EnvironmentService : IEnvironmentService
    {
        public string GetFolderPath(Environment.SpecialFolder folder)
        {
            return Environment.GetFolderPath(folder);
        }
    }
}