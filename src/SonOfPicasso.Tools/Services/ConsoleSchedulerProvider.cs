using System.Reactive.Concurrency;
using SonOfPicasso.Core.Scheduling;

namespace SonOfPicasso.Tools.Services
{
    public sealed class ConsoleSchedulerProvider : ISchedulerProvider
    {
        public IScheduler MainThreadScheduler => Scheduler.Default;

        public IScheduler TaskPool => TaskPoolScheduler.Default;
    }
}