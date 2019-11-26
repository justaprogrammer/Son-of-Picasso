using System.Reactive.Concurrency;
using ReactiveUI;
using SonOfPicasso.Core.Scheduling;

namespace SonOfPicasso.UI.WPF.Services
{
    public sealed class SchedulerProvider : ISchedulerProvider
    {
        public IScheduler MainThreadScheduler => RxApp.MainThreadScheduler;

        public IScheduler TaskPool => TaskPoolScheduler.Default;
    }
}