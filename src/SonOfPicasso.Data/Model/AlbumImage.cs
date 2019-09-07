namespace SonOfPicasso.Data.Model
{
    public class AlbumImage
    {
        public int AlbumImageId { get; set; }
        public int AlbumId { get; set; }
        public Album Album { get; set; }
        public int ImageId { get; set; }
        public Image Image { get; set; }
    }
}