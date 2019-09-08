namespace SonOfPicasso.Data.Model
{
    public class Album: IAlbum
    {
        public int Id { get; set; }

        public string Name { get; set; }
    }

    public interface IAlbum: IModel
    {
    }
}