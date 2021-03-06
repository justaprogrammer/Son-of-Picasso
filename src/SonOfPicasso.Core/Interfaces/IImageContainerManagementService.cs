﻿using System;
using System.Collections.Generic;
using System.Reactive;
using DynamicData;
using SonOfPicasso.Core.Model;
using SonOfPicasso.Core.Services;
using SonOfPicasso.Data.Model;

namespace SonOfPicasso.Core.Interfaces
{
    public interface IImageContainerManagementService: IDisposable
    {
        IObservable<Unit> ScanFolder(string path);
        IObservable<IImageContainer> CreateAlbum(ICreateAlbum createAlbum);
        IObservable<IImageContainer> AddImagesToAlbum(int albumId, IEnumerable<int> imageIds);
        IObservable<IImageContainer> AddImage(string path);
        IObservable<IImageContainer> DeleteImage(string path);
        IObservable<Unit> DeleteAlbum(int albumId);
        IConnectableCache<IImageContainer, string> ImageContainerCache { get; }
        IConnectableCache<ImageRef, string> FolderImageRefCache { get; }
        IObservable<Unit> ResetRules(IEnumerable<FolderRule> folderRules);
        IObservable<Unit> Start();
        void Stop();
        IObservable<ResetChanges> PreviewResetRulesChanges(IEnumerable<FolderRule> folderRules);
    }
}