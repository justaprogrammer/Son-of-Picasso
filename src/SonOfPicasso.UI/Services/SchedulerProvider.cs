using System.Reactive.Concurrency;
using PicasaDatabaseReader.Core;
using ReactiveUI;
using SonOfPicasso.Core.Scheduling;

namespace SonOfPicasso.UI.Services
{
    public sealed class SchedulerProvider : ISchedulerProvider, PicasaDatabaseReader.Core.Scheduling.ISchedulerProvider
    {
        public IScheduler MainThreadScheduler => RxApp.MainThreadScheduler;

        public IScheduler TaskPool => TaskPoolScheduler.Default;

        IScheduler PicasaDatabaseReader.Core.Scheduling.ISchedulerProvider.CurrentThread => throw new System.NotImplementedException();

        IScheduler PicasaDatabaseReader.Core.Scheduling.ISchedulerProvider.Immediate => throw new System.NotImplementedException();

        IScheduler PicasaDatabaseReader.Core.Scheduling.ISchedulerProvider.NewThread => throw new System.NotImplementedException();

        IScheduler PicasaDatabaseReader.Core.Scheduling.ISchedulerProvider.ThreadPool => TaskPool;
    }
}