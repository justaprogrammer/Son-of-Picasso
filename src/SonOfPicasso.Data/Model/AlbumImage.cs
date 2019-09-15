using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SonOfPicasso.Data.Model
{
    public class AlbumImage: IAlbumImage
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
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