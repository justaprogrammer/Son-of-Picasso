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
    public abstract class UnitTestsBase : TestsBase, IDisposable
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
            AutoResetEvent = new AutoResetEvent(false);
        }

        protected readonly AutoSubstitute AutoSubstitute;
        protected readonly Queue<IUnitOfWork> UnitOfWorkQueue;
        protected readonly MockFileSystem MockFileSystem;
        protected readonly TestSchedulerProvider TestSchedulerProvider;
        protected readonly AutoResetEvent AutoResetEvent;

        public void Dispose()
        {
            AutoSubstitute?.Dispose();
            AutoResetEvent?.Dispose();
        }

        protected void WaitOne(int timeout = 500)
        {
            AutoResetEvent.WaitOne(timeout).Should().BeTrue();
        }
    }
}