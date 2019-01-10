using System.Reactive.Concurrency;

namespace SonOfPicasso.UI.Scheduling
{
    public interface ISchedulerProvider
    {
        IScheduler CurrentThread { get; }
        IScheduler Immediate { get; }
        IScheduler NewThread { get; }
        IScheduler ThreadPool { get; }
        IScheduler TaskPool { get; }
        IScheduler MainThreadScheduler { get; }
    }
}