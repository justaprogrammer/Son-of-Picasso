﻿using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SonOfPicasso.Data.Model
{
    public class Image: IImage
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int DirectoryId { get; set; }
        
        public int ExifDataId { get; set; }

        public string Path { get; set; }

        public Directory Directory { get; set; }

        public ExifData ExifData { get; set; }

        public IList<AlbumImage> AlbumImages { get; set; }
    }

    public interface IImage: IModel
    {
    }
}