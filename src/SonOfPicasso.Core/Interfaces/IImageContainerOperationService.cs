using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Subjects;
using DynamicData;
using SonOfPicasso.Core.Model;
using SonOfPicasso.Core.Services;
using SonOfPicasso.Data.Model;

namespace SonOfPicasso.Core.Interfaces
{
    public interface IImageContainerOperationService
    {
        IObservable<Unit> ScanFolder(string path, IObservableCache<ImageRef, string> folderImageRefCache);
        IObservable<IImageContainer> CreateAlbum(ICreateAlbum createAlbum);
        IObservable<IImageContainer> GetAllImageContainers();
        IObservable<IImageContainer> AddImagesToAlbum(int albumId, IEnumerable<int> imageIds);
        IObservable<IImageContainer> AddImage(string path);
        IObservable<IImageContainer> DeleteImage(string path);
        IObservable<IImageContainer> UpdateImage(string path);
        IObservable<Unit> DeleteAlbum(int albumId);
        IObservable<ImageRef> AddOrUpdateImage(string path);
        IObservable<ResetChanges> PreviewRuleChangesEffect(IEnumerable<FolderRule> folderRules);
        IObservable<ResetChanges> ApplyRuleChanges(IEnumerable<FolderRule> folderRules);
        IObservable<IImageContainer>  GetFolderImageContainer(int folderId);
        IObservable<IImageContainer> GetAlbumImageContainer(int albumId);
        IObservable<ImageRef> ScanImageObservable { get; }
        IObservable<Unit> ScanImage(string path);
    }
}