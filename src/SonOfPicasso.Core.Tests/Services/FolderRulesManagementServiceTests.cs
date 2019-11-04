using System;
using System.Linq;
using Bogus;
using NSubstitute;
using SonOfPicasso.Core.Services;
using SonOfPicasso.Data.Interfaces;
using SonOfPicasso.Data.Model;
using SonOfPicasso.Testing.Common;
using SonOfPicasso.Testing.Common.Extensions;
using Xunit;
using Xunit.Abstractions;

namespace SonOfPicasso.Core.Tests.Services
{
    public class FolderRulesManagementServiceTests : UnitTestsBase
    {
        public FolderRulesManagementServiceTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
            _fakeFolderRule = new Faker<FolderRule>()
                .RuleFor(rule => rule.Id, 0)
                .RuleFor(rule => rule.Path, Faker.System.DirectoryPathWindows())
                .RuleFor(rule => rule.Action, faker => faker.PickRandom<FolderRuleActionEnum>());
        }

        private readonly Faker<FolderRule> _fakeFolderRule;

        [Fact]
        public void ShouldGetFolderManagmentRules()
        {
            var unitOfWork = Substitute.For<IUnitOfWork>();
            unitOfWork.FolderRuleRepository.Get()
                .Returns(_fakeFolderRule.GenerateLazy(3));

            UnitOfWorkQueue.Enqueue(unitOfWork);

            var folderRulesManagementService = AutoSubstitute.Resolve<FolderRulesManagementService>();
            folderRulesManagementService.GetFolderManagementRules()
                .Subscribe(list =>
                {
                }, () =>
                {
                    AutoResetEvent.Set();
                });

            TestSchedulerProvider.TaskPool.AdvanceBy(1);

            unitOfWork.FolderRuleRepository
                .Received(1)
                .Get();

            WaitOne();
        }

        [Fact]
        public void ShouldResetFolderManagmentRules()
        {
            var originals = _fakeFolderRule.GenerateLazy(3)
                .ToArray();

            var unitOfWork = Substitute.For<IUnitOfWork>();
            
            unitOfWork.FolderRuleRepository.Get()
                .Returns(originals);

            UnitOfWorkQueue.Enqueue(unitOfWork);

            var folderRulesManagementService = AutoSubstitute.Resolve<FolderRulesManagementService>();
            folderRulesManagementService.ResetFolderManagementRules(_fakeFolderRule.GenerateLazy(3))
                .Subscribe(unit =>
                    {
                    },
                    () =>
                    {
                        AutoResetEvent.Set();
                    });

            TestSchedulerProvider.TaskPool.AdvanceBy(1);

            unitOfWork.FolderRuleRepository
                .Received(1)
                .Get();

            unitOfWork.FolderRuleRepository
                .ReceivedWithAnyArgs(3)
                .Delete(default);

            foreach (var folderRule in originals)
            {
                unitOfWork.FolderRuleRepository
                    .Received(1)
                    .Delete(folderRule);
            }

            WaitOne();
        }
    }
}