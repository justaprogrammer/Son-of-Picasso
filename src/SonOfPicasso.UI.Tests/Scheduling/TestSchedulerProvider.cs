using System.Reactive.Concurrency;
using Microsoft.Reactive.Testing;
using SonOfPicasso.Core.Scheduling;

namespace SonOfPicasso.UI.Tests.Scheduling
{
    public sealed class TestSchedulerProvider : ISchedulerProvider
    {
        public TestScheduler TaskPool { get; } = new TestScheduler();
        public TestScheduler MainThreadScheduler { get; } = new TestScheduler();

        IScheduler ISchedulerProvider.TaskPool => TaskPool;
        IScheduler ISchedulerProvider.MainThreadScheduler => MainThreadScheduler;
    }
}