namespace SonOfPicasso.Data.Model
{
    public class AlbumImage: IAlbumImage
    {
        public int Id { get; set; }
        public int AlbumId { get; set; }
        public Album Album { get; set; }
        public int ImageId { get; set; }
        public Image Image { get; set; }
    }

    public interface IAlbumImage: IModel
    {
    }
}