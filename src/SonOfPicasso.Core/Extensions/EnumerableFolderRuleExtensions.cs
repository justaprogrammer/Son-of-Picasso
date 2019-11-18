using System;
using System.Collections.Generic;
using System.Linq;
using SonOfPicasso.Data.Model;

namespace SonOfPicasso.Core.Extensions
{
    public static class EnumerableFolderRuleExtensions
    {
        public static Dictionary<string, List<FolderRule>> GetTopLevelItemDictionary(this IEnumerable<FolderRule> folderRules)
        {
            var itemsDictionary = new Dictionary<string, List<FolderRule>>();

            string firstRoot = null;
            folderRules = folderRules
                .OrderBy(rule => rule.Path, StringComparer.InvariantCultureIgnoreCase);

            foreach (var folderRule in folderRules)
                if (firstRoot == null || !folderRule.Path.StartsWith(firstRoot))
                {
                    firstRoot = folderRule.Path;
                    itemsDictionary[firstRoot] = new List<FolderRule>();
                }
                else
                {
                    itemsDictionary[firstRoot].Add(folderRule);
                }

            return itemsDictionary;
        }
    }
}