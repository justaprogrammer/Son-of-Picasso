using System.Reactive.Concurrency;

namespace SonOfPicasso.Core.Scheduling
{
    public interface ISchedulerProvider
    {
        IScheduler TaskPool { get; }
        IScheduler MainThreadScheduler { get; }
    }
}