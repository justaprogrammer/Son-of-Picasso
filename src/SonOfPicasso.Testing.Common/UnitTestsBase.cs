using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Threading;
using Autofac;
using Autofac.Extras.NSubstitute;
using AutofacSerilogIntegration;
using FluentAssertions;
using NSubstitute;
using SonOfPicasso.Core.Scheduling;
using SonOfPicasso.Data.Interfaces;
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

            FileSystemWatcherQueue = new Queue<IFileSystemWatcher>();

            FileSystemWatcherFactory = Substitute.For<IFileSystemWatcherFactory>();
            FileSystemWatcherFactory.FromPath(default)
                .ReturnsForAnyArgs(info => FileSystemWatcherQueue.Dequeue());

            MockFileSystem = new MockFileSystem();
            MockFileSystem.FileSystemWatcher = FileSystemWatcherFactory;
            AutoSubstitute.Provide<IFileSystem>(MockFileSystem);
        }

        protected readonly AutoSubstitute AutoSubstitute;
        protected readonly Queue<IUnitOfWork> UnitOfWorkQueue;
        protected readonly MockFileSystem MockFileSystem;
        protected readonly TestSchedulerProvider TestSchedulerProvider;
        protected Queue<IFileSystemWatcher> FileSystemWatcherQueue;
        protected IFileSystemWatcherFactory FileSystemWatcherFactory;

        public override void Dispose()
        {
            base.Dispose();
            AutoSubstitute?.Dispose();
        }
    }
}