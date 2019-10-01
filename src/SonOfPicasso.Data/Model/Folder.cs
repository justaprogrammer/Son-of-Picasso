using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SonOfPicasso.Data.Model
{
    public class Folder: IFolder
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public string Path { get; set; }
        
        public DateTime Date { get; set; }

        public List<Image> Images { get; set; }
    }

    public interface IFolder: IModel
    {
    }
}