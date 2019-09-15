using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Threading;
using Autofac;
using Autofac.Extras.NSubstitute;
using AutofacSerilogIntegration;
using FluentAssertions;
using SonOfPicasso.Core.Scheduling;
using SonOfPicasso.Data.Repository;
using SonOfPicasso.Testing.Common.Scheduling;
using Xunit.Abstractions;

namespace SonOfPicasso.Testing.Common
{
    public abstract class UnitTestsBase : TestsBase
    {
        protected UnitTestsBase(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterLogger();

            AutoSubstitute = new AutoSubstitute(containerBuilder);

            TestSchedulerProvider = new TestSchedulerProvider();
            AutoSubstitute.Provide<ISchedulerProvider>(TestSchedulerProvider);

            UnitOfWorkQueue = new Queue<IUnitOfWork>();
            AutoSubstitute.Provide<Func<IUnitOfWork>>(() => UnitOfWorkQueue.Dequeue());

            MockFileSystem = new MockFileSystem();
            AutoSubstitute.Provide<IFileSystem>(MockFileSystem);
        }

        protected readonly AutoSubstitute AutoSubstitute;
        protected readonly Queue<IUnitOfWork> UnitOfWorkQueue;
        protected readonly MockFileSystem MockFileSystem;
        protected readonly TestSchedulerProvider TestSchedulerProvider;

        public override void Dispose()
        {
            base.Dispose();
            AutoSubstitute?.Dispose();
        }
    }
}