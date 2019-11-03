using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using Serilog;
using SonOfPicasso.Core.Interfaces;
using SonOfPicasso.Core.Scheduling;
using SonOfPicasso.Data.Interfaces;
using SonOfPicasso.Data.Model;

namespace SonOfPicasso.Core.Services
{
    public class FolderRulesManagementService : IFolderRulesManagementService
    {
        private readonly ILogger _logger;
        private readonly ISchedulerProvider _schedulerProvider;
        private readonly Func<IUnitOfWork> _unitOfWorkFactory;

        public FolderRulesManagementService(ILogger logger,
            Func<IUnitOfWork> unitOfWorkFactory,
            ISchedulerProvider schedulerProvider)
        {
            _logger = logger;
            _unitOfWorkFactory = unitOfWorkFactory;
            _schedulerProvider = schedulerProvider;
        }

        public IObservable<Unit> ResetFolderManagementRules(IEnumerable<IFolderRuleInput> folderRuleInputs)
        {
            return Observable.Defer(() => Observable.Return(FolderRulesFactory.ComputeRuleset(folderRuleInputs)))
                .SelectMany(ResetFolderManagementRules)
                .SubscribeOn(_schedulerProvider.TaskPool);
        }

        public IObservable<Unit> ResetFolderManagementRules(IEnumerable<FolderRule> folderRules)
        {
            return Observable.Defer(() =>
                {
                    using var unitOfWork = _unitOfWorkFactory();
                    var oldFolderRules = unitOfWork.FolderRuleRepository.Get().ToArray();
                    foreach (var folderRule in oldFolderRules) unitOfWork.FolderRuleRepository.Delete(folderRule);

                    foreach (var folderRule in folderRules) unitOfWork.FolderRuleRepository.Insert(folderRule);

                    unitOfWork.Save();

                    return Observable.Return(Unit.Default);
                })
                .SubscribeOn(_schedulerProvider.TaskPool);
        }

        public IObservable<IList<FolderRule>> GetFolderManagementRules()
        {
            return Observable.Defer(() =>
                {
                    using var unitOfWork = _unitOfWorkFactory();
                    var folderRules = unitOfWork.FolderRuleRepository
                        .Get().ToArray();
                 
                    return Observable.Return(folderRules);
                })
                .SubscribeOn(_schedulerProvider.TaskPool);
        }
    }
}