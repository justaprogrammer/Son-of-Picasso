using System.Collections.Generic;

namespace SonOfPicasso.Data.Model
{
    public class Directory: IDirectory
    {
        public int Id { get; set; }

        public string Path { get; set; }

        public List<Image> Images { get; set; }
    }

    public interface IDirectory: IModel
    {

    }
}