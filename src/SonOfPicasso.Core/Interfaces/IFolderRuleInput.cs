using System.Collections.Generic;
using SonOfPicasso.Data.Model;

namespace SonOfPicasso.Core.Interfaces
{
    public interface IFolderRuleInput
    {
        string FullName { get; }
        FolderRuleActionEnum ManageFolderState { get; set; }
        IList<IFolderRuleInput> Children { get; }
    }
}