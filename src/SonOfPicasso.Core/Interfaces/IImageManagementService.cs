using System;
using System.Reactive;
using SonOfPicasso.Core.Models;
using SonOfPicasso.Data.Model;

namespace SonOfPicasso.Core.Interfaces
{
    public interface IImageManagementService
    {
        IObservable<Image[]> ScanFolder(string path);
    }
}