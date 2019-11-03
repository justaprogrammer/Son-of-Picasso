using System.Collections.Generic;
using System.Linq;
using SonOfPicasso.Core.Interfaces;
using SonOfPicasso.Data.Model;

namespace SonOfPicasso.Core.Services
{
    public static class FolderRulesFactory
    {
        public static IEnumerable<FolderRule> ComputeRuleset(IEnumerable<IFolderRuleInput> manageFolderViewModels)
        {
            return manageFolderViewModels
                .Select(ComputeRuleset)
                .SelectMany(rules => rules);
        }

        private static IEnumerable<FolderRule> ComputeRuleset(IFolderRuleInput folderRuleViewModel)
        {
            return ComputeInternal(folderRuleViewModel, null);
        }

        private static IEnumerable<FolderRule> ComputeInternal(IFolderRuleInput folderRuleViewModel,
            FolderRuleActionEnum? state)
        {
            if (folderRuleViewModel.ManageFolderState != state)
            {
                if (state.HasValue || folderRuleViewModel.ManageFolderState != FolderRuleActionEnum.Remove)
                {
                    yield return new FolderRule
                    {
                        Path = folderRuleViewModel.FullName,
                        Action = folderRuleViewModel.ManageFolderState
                    };

                    state = folderRuleViewModel.ManageFolderState;
                }
            }

            foreach (var child in folderRuleViewModel.Children)
            foreach (var manageFolderRule in ComputeInternal(child, state))
                yield return manageFolderRule;
        }
    }
}