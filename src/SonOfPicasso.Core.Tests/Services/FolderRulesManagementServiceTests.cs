using System;
using System.Linq;
using Bogus;
using FluentAssertions;
using FluentAssertions.Execution;
using NSubstitute;
using SonOfPicasso.Core.Services;
using SonOfPicasso.Data.Interfaces;
using SonOfPicasso.Data.Model;
using SonOfPicasso.Data.Repository;
using SonOfPicasso.Testing.Common;
using SonOfPicasso.Testing.Common.Extensions;
using Xunit;
using Xunit.Abstractions;

namespace SonOfPicasso.Core.Tests.Services
{
    public class FolderRulesManagementServiceTests
    {
        public class AddFolderManagementRule : UnitTestsBase
        {
            public AddFolderManagementRule(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
            {
            }

            private FolderRule[] ShouldModifyFolderRules(FolderRule[] existingRules, FolderRule addRule)
            {
                var unitOfWork1 = Substitute.For<IUnitOfWork>();
                unitOfWork1.FolderRuleRepository.Get()
                    .Returns(existingRules);

                UnitOfWorkQueue.Enqueue(unitOfWork1);

                var unitOfWork2 = Substitute.For<IUnitOfWork>();
                unitOfWork2.FolderRuleRepository.Get()
                    .Returns(existingRules);

                UnitOfWorkQueue.Enqueue(unitOfWork2);

                var folderRulesManagementService = AutoSubstitute.Resolve<FolderRulesManagementService>();

                folderRulesManagementService.AddFolderManagementRule(addRule)
                    .Subscribe(
                        list => { },
                        () =>
                        {
                            AutoResetEvent.Set();
                        });

                TestSchedulerProvider.TaskPool.AdvanceBy(1);
                TestSchedulerProvider.TaskPool.AdvanceBy(1);
                TestSchedulerProvider.TaskPool.AdvanceBy(1);

                AutoResetEvent.WaitOne();

                unitOfWork1.DidNotReceive().Save();

                using (new AssertionScope())
                {
                    unitOfWork2.FolderRuleRepository.ReceivedWithAnyArgs(existingRules.Length).Delete(default);
                    foreach (var existingRule in existingRules)
                        unitOfWork2.FolderRuleRepository.Received(1).Delete(existingRule);
                }

                var insertedFolderRules = unitOfWork2.FolderRuleRepository.ReceivedCalls()
                    .Where(call => call.GetMethodInfo().Name.Equals(nameof(IGenericRepository<FolderRule>.Insert)))
                    .Select(call => call.GetArguments().First())
                    .Cast<FolderRule>()
                    .ToArray();

                unitOfWork2.Received(1).Save();

                return insertedFolderRules;
            }

            [Fact]
            public void ShouldNotAddChildWithSameAction()
            {
                var existingRules = new[]
                {
                    new FolderRule
                    {
                        Path = "c:\\Stanley\\Pictures",
                        Action = FolderRuleActionEnum.Always
                    },
                    new FolderRule
                    {
                        Path = "c:\\Stanley\\Screenshots",
                        Action = FolderRuleActionEnum.Once
                    },
                    new FolderRule
                    {
                        Path = "D:\\Other\\Path",
                        Action = FolderRuleActionEnum.Once
                    }
                };

                var addRule = new FolderRule
                {
                    Path = "c:\\Stanley\\Pictures\\2019-01-20",
                    Action = FolderRuleActionEnum.Always
                };

                var newFolderRules = ShouldModifyFolderRules(existingRules, addRule);

                newFolderRules.Should().BeEquivalentTo(existingRules);
            }

            [Fact]
            public void ShouldAddChildWithDifferentAction()
            {
                var existingRules = new[]
                {
                    new FolderRule
                    {
                        Path = "c:\\Stanley\\Pictures",
                        Action = FolderRuleActionEnum.Always
                    },
                    new FolderRule
                    {
                        Path = "c:\\Stanley\\Screenshots",
                        Action = FolderRuleActionEnum.Once
                    },
                    new FolderRule
                    {
                        Path = "D:\\Other\\Path",
                        Action = FolderRuleActionEnum.Once
                    }
                };

                var addRule = new FolderRule
                {
                    Path = "c:\\Stanley\\Pictures\\2019-01-20",
                    Action = FolderRuleActionEnum.Remove
                };

                var expected = new[]
                {
                    new FolderRule
                    {
                        Path = "c:\\Stanley\\Pictures",
                        Action = FolderRuleActionEnum.Always
                    },
                    new FolderRule
                    {
                        Path = "c:\\Stanley\\Pictures\\2019-01-20",
                        Action = FolderRuleActionEnum.Remove
                    },
                    new FolderRule
                    {
                        Path = "c:\\Stanley\\Screenshots",
                        Action = FolderRuleActionEnum.Once
                    },
                    new FolderRule
                    {
                        Path = "D:\\Other\\Path",
                        Action = FolderRuleActionEnum.Once
                    }
                };

                var newFolderRules = ShouldModifyFolderRules(existingRules, addRule);

                newFolderRules.Should().BeEquivalentTo(expected);
            }

            [Fact]
            public void ShouldAddNewAction()
            {
                var existingRules = new[]
                {
                    new FolderRule
                    {
                        Path = "c:\\Stanley\\Pictures",
                        Action = FolderRuleActionEnum.Always
                    },
                    new FolderRule
                    {
                        Path = "D:\\Other\\Path",
                        Action = FolderRuleActionEnum.Once
                    }
                };

                var addRule = new FolderRule
                {
                    Path = "c:\\Stanley\\Screenshots",
                    Action = FolderRuleActionEnum.Once
                };

                var expected = new[]
                {
                    new FolderRule
                    {
                        Path = "c:\\Stanley\\Pictures",
                        Action = FolderRuleActionEnum.Always
                    },
                    new FolderRule
                    {
                        Path = "c:\\Stanley\\Screenshots",
                        Action = FolderRuleActionEnum.Once
                    },
                    new FolderRule
                    {
                        Path = "D:\\Other\\Path",
                        Action = FolderRuleActionEnum.Once
                    }
                };

                var newFolderRules = ShouldModifyFolderRules(existingRules, addRule);

                newFolderRules.Should().BeEquivalentTo(expected);
            }
        }

        public class BasicOperation : UnitTestsBase
        {
            public BasicOperation(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
            {
                _fakeFolderRule = new Faker<FolderRule>()
                    .RuleFor(rule => rule.Id, 0)
                    .RuleFor(rule => rule.Path, Faker.System.DirectoryPathWindows())
                    .RuleFor(rule => rule.Action, faker => faker.PickRandom<FolderRuleActionEnum>());
            }

            private readonly Faker<FolderRule> _fakeFolderRule;

            [Fact]
            public void ShouldGetExistingRules()
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
            public void ShouldResetRules()
            {
                var originals = _fakeFolderRule.GenerateLazy(3)
                    .ToArray();

                var unitOfWork = Substitute.For<IUnitOfWork>();

                unitOfWork.FolderRuleRepository.Get()
                    .Returns(originals);

                UnitOfWorkQueue.Enqueue(unitOfWork);

                var input = _fakeFolderRule.GenerateLazy(3);

                var folderRulesManagementService = AutoSubstitute.Resolve<FolderRulesManagementService>();
                folderRulesManagementService.ResetFolderManagementRules(input)
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
                    unitOfWork.FolderRuleRepository
                        .Received(1)
                        .Delete(folderRule);

                WaitOne();
            }
        }
    }
}