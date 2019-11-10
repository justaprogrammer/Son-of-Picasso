using System.Collections.Generic;
using SonOfPicasso.Data.Model;

namespace SonOfPicasso.Core.Interfaces
{
    public interface IFolderRuleInput
    {
        string Path { get; }
        FolderRuleActionEnum FolderRuleAction { get; }
        IList<IFolderRuleInput> Children { get; }
    }
}