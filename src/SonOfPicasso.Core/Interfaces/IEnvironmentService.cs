using System;

namespace SonOfPicasso.Core.Interfaces
{
    public interface IEnvironmentService
    {
        string GetFolderPath(Environment.SpecialFolder folder);
        string GetEnvironmentVariable(string variable);
    }
}