using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using DynamicData;
using Serilog;
using SonOfPicasso.Core.Interfaces;
using SonOfPicasso.Core.Scheduling;
using SonOfPicasso.Data.Interfaces;
using SonOfPicasso.Data.Model;

namespace SonOfPicasso.Core.Services
{
    public class FolderRulesManagementService : IFolderRulesManagementService
    {
        private readonly IFileSystem _fileSystem;
        private readonly ILogger _logger;
        private readonly ISchedulerProvider _schedulerProvider;
        private readonly Func<IUnitOfWork> _unitOfWorkFactory;

        public FolderRulesManagementService(ILogger logger,
            Func<IUnitOfWork> unitOfWorkFactory,
            IFileSystem fileSystem,
            ISchedulerProvider schedulerProvider)
        {
            _logger = logger;
            _unitOfWorkFactory = unitOfWorkFactory;
            _fileSystem = fileSystem;
            _schedulerProvider = schedulerProvider;
        }

        public IObservable<Unit> ResetFolderManagementRules(IEnumerable<IFolderRuleInput> folderRuleInputs)
        {
            return Observable.Return(folderRuleInputs.ToList())
                .Select(FolderRulesFactory.ComputeRuleset)
                .SelectMany(ResetFolderManagementRules)
                .SubscribeOn(_schedulerProvider.TaskPool);
        }

        public IObservable<IList<FolderRule>> GetFolderManagementRules()
        {
            return Observable.Defer(() =>
                {
                    using var unitOfWork = _unitOfWorkFactory();
                    var folderRules = unitOfWork.FolderRuleRepository
                        .Get()
                        .ToArray();

                    return Observable.Return(folderRules);
                })
                .SubscribeOn(_schedulerProvider.TaskPool);
        }

        public IObservable<Unit> ResetFolderManagementRules(IEnumerable<FolderRule> folderRules)
        {
            return Observable.Return(folderRules.ToList())
                .Select(list =>
                {
                    using var unitOfWork = _unitOfWorkFactory();
                    var oldFolderRules = unitOfWork.FolderRuleRepository.Get().ToArray();
                    foreach (var folderRule in oldFolderRules) unitOfWork.FolderRuleRepository.Delete(folderRule);

                    foreach (var folderRule in list) unitOfWork.FolderRuleRepository.Insert(folderRule);

                    unitOfWork.Save();

                    return Unit.Default;
                });
        }

        public IObservable<Unit> AddFolderManagementRule(FolderRule folderRule)
        {
            return Observable.Defer(GetFolderManagementRules)
                .Select(list => list.Append(folderRule).ToArray())
                .Select(rules => CreateInputs(rules))
                .SelectMany(ResetFolderManagementRules)
                .SubscribeOn(_schedulerProvider.TaskPool);
        }

        private FolderRuleInput[] CreateInputs(IList<FolderRule> folderManagementRules)
        {
            var pathRuleDictionary =
                folderManagementRules.ToDictionary(rule => rule.Path, rule => rule.Action);

            var knownPaths = folderManagementRules
                .Select(rule => rule.Path)
                .SelectMany(ParsePath)
                .Distinct()
                .OrderBy(s => s)
                .ToArray();

            var knownPathsGroupedByParent = knownPaths
                .GroupBy(s => _fileSystem.DirectoryInfo.FromDirectoryName(s).Parent?.FullName ?? string.Empty)
                .ToDictionary(grouping => grouping.Key, grouping => grouping.ToArray());

            var folderRuleInputs = knownPathsGroupedByParent[string.Empty]
                .Select(s => CreateFolderRuleInput(s, knownPathsGroupedByParent, pathRuleDictionary))
                .ToArray();

            return folderRuleInputs;
        }

        private FolderRuleInput CreateFolderRuleInput(string fullName, Dictionary<string, string[]> dictionary,
            Dictionary<string, FolderRuleActionEnum> dic3)
        {
            if (!dic3.TryGetValue(fullName, out var folderRuleAction)) folderRuleAction = FolderRuleActionEnum.Remove;

            var folderRuleInput = new FolderRuleInput
            {
                Path = fullName,
                FolderRuleAction = folderRuleAction
            };

            if (dictionary.TryGetValue(fullName, out var children))
                folderRuleInput.Children.AddRange(children.Select(child =>
                    CreateFolderRuleInput(child, dictionary, dic3)));

            return folderRuleInput;
        }

        private IList<string> ParsePath(string path)
        {
            var result = new List<string>();

            var directoryInfo = _fileSystem.DirectoryInfo.FromDirectoryName(path);
            while (directoryInfo != null)
            {
                result.Add(directoryInfo.FullName);
                directoryInfo = directoryInfo.Parent;
            }

            return result;
        }

        private class FolderRuleInput : IFolderRuleInput
        {
            public string Path { get; set; }

            public FolderRuleActionEnum FolderRuleAction { get; set; }

            public IList<IFolderRuleInput> Children { get; } = new List<IFolderRuleInput>();
        }
    }
}