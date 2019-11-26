using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SonOfPicasso.Data.Model
{
    public class FolderRule: IFolderRule
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public string Path { get; set; }

        public FolderRuleActionEnum Action { get; set; }
    }

    public interface IFolderRule: IModel
    {
    }
}