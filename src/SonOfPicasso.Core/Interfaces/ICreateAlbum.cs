using System;

namespace SonOfPicasso.Core.Interfaces
{
    public interface ICreateAlbum
    {
        string AlbumName { get; }
        DateTime AlbumDate { get; }
    }
}