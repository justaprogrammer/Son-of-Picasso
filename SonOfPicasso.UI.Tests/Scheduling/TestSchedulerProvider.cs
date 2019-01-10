using System.Reactive.Concurrency;
using Microsoft.Reactive.Testing;
using SonOfPicasso.UI.Scheduling;

namespace SonOfPicasso.UI.Tests.Scheduling
{
    public sealed class TestSchedulerProvider : ISchedulerProvider
    {
        public TestScheduler CurrentThread { get; } = new TestScheduler();
        public TestScheduler Immediate { get; } = new TestScheduler();
        public TestScheduler NewThread { get; } = new TestScheduler();
        public TestScheduler ThreadPool { get; } = new TestScheduler();
        public TestScheduler TaskPool { get; } = new TestScheduler();

        IScheduler ISchedulerProvider.CurrentThread => CurrentThread;
        IScheduler ISchedulerProvider.Immediate => Immediate;
        IScheduler ISchedulerProvider.NewThread => NewThread;
        IScheduler ISchedulerProvider.ThreadPool => ThreadPool;
        IScheduler ISchedulerProvider.TaskPool => TaskPool;
    }
}