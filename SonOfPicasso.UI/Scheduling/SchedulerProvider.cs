using System.Reactive.Concurrency;
using ReactiveUI;

namespace SonOfPicasso.UI.Scheduling
{
    public sealed class SchedulerProvider : ISchedulerProvider
    {
        public IScheduler MainThreadScheduler => RxApp.MainThreadScheduler;

        public IScheduler CurrentThread => Scheduler.CurrentThread;

        public IScheduler Immediate => Scheduler.Immediate;

        public IScheduler NewThread => NewThreadScheduler.Default;

        public IScheduler ThreadPool => ThreadPoolScheduler.Instance;

        public IScheduler TaskPool => TaskPoolScheduler.Default;
    }
}