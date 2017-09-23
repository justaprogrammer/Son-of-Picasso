using System.Reactive.Concurrency;

namespace PicasaReboot.Core.Scheduling
{
    public sealed class SchedulerProvider : ISchedulerProvider
    {
        public IScheduler CurrentThread => Scheduler.CurrentThread;

        public IScheduler Dispatcher => DispatcherScheduler.Current;

        public IScheduler Immediate => Scheduler.Immediate;

        public IScheduler NewThread => NewThreadScheduler.Default;

        public IScheduler ThreadPool => Scheduler.Default;
    }
}